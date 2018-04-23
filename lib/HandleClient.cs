using lib.HTTPObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using lib.Streams;
using System.Net;

namespace lib
{
    public class HandleClient
    {
        private static int nrOfClientsMade = 0;
        TcpClient tcpClient;
        SslStream sslStream;
        StreamReader streamReader;
        StreamWriter streamWriter;
        //NetworkStream networkStream;
        BinaryReader binaryReader;
        BinaryWriter binaryWriter;
        private object streamreaderlock = new object();
        private object streamwriterlock = new object();
        private object binaryreaderlock = new object();
        private object binarywriterlock = new object();
        bool HttpUpgraded = false;
        StreamHandler streamHandler;
        public Http2.Hpack.Encoder hpackEncoder { get; private set; }
        public Http2.Hpack.Decoder hpackDecoder { get; private set; }
        public bool Connected { get; private set; } = true;
        public int ClientPort { get; private set; }
        private byte[] http2ConnectionPreface = { 0x50, 0x52, 0x49, 0x20, 0x2a, 0x20, 0x48, 0x54, 0x54, 0x50, 0x2f, 0x32, 0x2e, 0x30, 0x0d, 0x0a, 0x0d, 0x0a, 0x53, 0x4d, 0x0d, 0x0a, 0x0d, 0x0a };

        public HandleClient()
        {
            Console.WriteLine("Handleclient made: " + ++nrOfClientsMade);
            
        }

        
        public async void StartThreadForClient(TcpClient tcpClient, int port, X509Certificate2 certificate = null)
        {
            this.tcpClient = tcpClient;

            try
            {
                if(certificate != null)
                {
                    sslStream = new SslStream(tcpClient.GetStream(), false, App_CertificateValidation);
                    SslServerAuthenticationOptions options = new SslServerAuthenticationOptions();
                    options.ApplicationProtocols = new List<SslApplicationProtocol>()
                    {
                        SslApplicationProtocol.Http2,
                        SslApplicationProtocol.Http11
                    };
                    options.ServerCertificate = certificate;
                    options.EnabledSslProtocols = SslProtocols.Tls12;
                    options.ClientCertificateRequired = false;
                    options.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;

                    await sslStream.AuthenticateAsServerAsync(options, CancellationToken.None);
                    if (sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2) InitUpgradeToHttp2();
                    streamReader = new StreamReader(sslStream);
                    streamWriter = new StreamWriter(sslStream);
                    binaryReader = new BinaryReader(sslStream);
                    binaryWriter = new BinaryWriter(sslStream);
                    Console.WriteLine("New client connected with TLS 1.2---------------------------------");
                }
                else
                {
                    streamReader = new StreamReader(tcpClient.GetStream());
                    streamWriter = new StreamWriter(tcpClient.GetStream());
                    binaryReader = new BinaryReader(tcpClient.GetStream());
                    binaryWriter = new BinaryWriter(tcpClient.GetStream());
                    Console.WriteLine("New client connected---------------------------------");
                }
                ClientPort = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port;
                Console.WriteLine("From port " + ClientPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Handshake failed... \n" + ex.ToString());
                tcpClient.Close();
                return;
            }

            Thread t = new Thread(StartReadingAsync);
            t.Start();

        }

        private async void StartReadingAsync()
        {
            while (Connected)
            {
                if (!HttpUpgraded)
                {
                     await ReadStreamToString((msg) => {
                        // a request has ben recived
                        HTTP1Request req = new HTTP1Request(msg);
                        Console.WriteLine(req.ToString());
                        Response res = Response.From(req);
                        Console.WriteLine(res.ToString());
                        Task.Run(() => WriteResponse(res));
                         // todo vent på preface
                         // force upgrade on browser type
                         // User-Agent : Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36
                         if (req.IsUpgradeTo2) // || req.HeaderLines.Contains(new KeyValuePair<string, string>("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36")))
                        {
                            InitUpgradeToHttp2();
                            streamHandler.SendFrame(new HTTP2Frame(0).AddSettingsPayload(new(ushort, uint)[0])); // connection preface
                            streamHandler.RespondWithFirstHTTP2(req.HttpUrl);
                        }
                    }, () => {
                        Thread.Sleep(100);
                    });
                }
                else
                {
                    await ReadStreamToFrameBytes((framedata) => {
                        // sjekk om preface eller ramme
                        if (IsPreface(framedata))
                        {
                            Console.WriteLine("Connectionpreface recived");
                            streamHandler.SendFrame(new HTTP2Frame(0).AddSettingsPayload(new(ushort, uint)[0], false));
                        }
                        else
                        {
                            HTTP2Frame frame = new HTTP2Frame(framedata);
                            if (streamHandler.IncomingStreamExist(frame.StreamIdentifier))
                            {
                                streamHandler.AddIncomingFrame(frame);
                            }
                            else
                            {
                                streamHandler.AddStreamToIncomming(new HTTP2Stream((uint)frame.StreamIdentifier, StreamState.Open));
                                streamHandler.AddIncomingFrame(frame);
                            }
                        }
                    }, () => {
                        Thread.Sleep(100);
                    });
                }
                if (tcpClient.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] checkConn = new byte[1];
                    if (!tcpClient.Connected)
                    {
                        Console.WriteLine($"TcpClient disconnected from {ClientPort}");
                        Connected = false;
                    }
                    //else if (tcpClient.Client.Receive(checkConn, SocketFlags.Peek) == 0)
                    //{
                    //    Console.WriteLine($"TcpClient disconnected from {ClientPort}");
                    //    Connected = false;
                    //}
                }
            }
            Close();
        }

        private void InitUpgradeToHttp2()
        {
            hpackEncoder = new Http2.Hpack.Encoder();
            hpackDecoder = new Http2.Hpack.Decoder();
            streamHandler = new StreamHandler(this);
            streamHandler.StartSendThread();
            HttpUpgraded = true;
        }


        private async Task ReadStreamToString(Action<string> onRequest, Action onNoRequest)
        {
            try
            {
                string msg = "";
                while (streamReader.Peek() != -1)
                {
                    msg += await streamReaderReadLineSync() + "\n";
                }
                if(msg.Length > 5)
                {
                    onRequest(msg);
                }
                else
                {
                    onNoRequest();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadStreamToString()\n" + ex);
            }
        }

        private async Task ReadStreamToFrameBytes(Action<byte[]> framedata, Action onEmptyFrame)
        {
            byte[] myReadBuffer = new byte[3];
            int numberOfBytesRead = 0;
            // lock (binaryreaderlock)
            // {
            //     numberOfBytesRead = binaryReader.Read(myReadBuffer, 0, myReadBuffer.Length);
            // }
            //numberOfBytesRead = binaryReader.Read(myReadBuffer, 0, myReadBuffer.Length);
            if (tcpClient.GetStream().DataAvailable)
            {
                lock (binaryreaderlock)
                {
                    numberOfBytesRead = binaryReader.Read(myReadBuffer, 0, myReadBuffer.Length);

                }
            }
            //numberOfBytesRead = await binaryReaderReadSync(myReadBuffer);

            if (numberOfBytesRead == 3) // myReadBuffer != null && myReadBuffer.Length == 3 && myReadBuffer[0] != 0 && myReadBuffer[1] != 0 && myReadBuffer[2] != 0)
            {
                bool littleEndian = BitConverter.IsLittleEndian;
                byte[] source = myReadBuffer;
                byte[] target = new byte[4];
                for (int i = 0; i < 3; i++)
                {
                    int j = littleEndian ? 0 : 3;
                    target[j] = source[i];
                    j += littleEndian ? 1 : -1;
                }
                int length = BitConverter.ToInt32(target, 0) + 9;
                //Console.WriteLine("Lengt of recived frame: " + length);
                byte[] data = new byte[length];
                data[0] = source[0];
                data[1] = source[1];
                data[2] = source[2];
                lock (binaryreaderlock)
                {
                    binaryReader.Read(data, 3, length - 3);
                }
                framedata(data);
            }
            else
            {
                onEmptyFrame();
            }
            
        }

        private async Task<int> binaryReaderReadSync(byte[] buffer)
        {
            int nr = 0;
            lock (binaryreaderlock)
            {
                nr = binaryReader.Read(buffer, 0, buffer.Length);

            }
            return nr;
        }

        private async Task<string> streamReaderReadLineSync()
        {
            lock (streamreaderlock)
            {
                return streamReader.ReadLine();
            }
        }

        internal async Task WriteFrame(HTTP2Frame frame)
        {
            try
            {
                Console.WriteLine("Sender ramme: ");
                Console.WriteLine(frame.ToString());
                lock (binaryWriter)
                {
                    binaryWriter.Flush();
                    binaryWriter.Write(frame.GetBytes(), 0, frame.GetBytes().Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("WriteFrame()\n" + ex);
            }
        }
        
        private async Task WriteResponse(Response r)
        {
            try
            {
                if (r == null) return;
                streamWriterFlushSync();
                streamWriterWriteSync(r.ToString());
                // streamWriter.Flush();
                if (r.Data == null) return;
                streamWriterWriteSync(r.Data, 0, r.Data.Length);
                streamWriterFlushSync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("WriteResponse()\n" + ex);
            }
        }

        private void streamWriterWriteSync(string s)
        {
            lock (streamwriterlock)
            {
                streamWriter.Write(s);
            }
        }

        private void streamWriterWriteSync(char[] data, int index, int length)
        {
            lock (streamwriterlock)
            {
                streamWriter.Write(data, index, length);
            }
        }

        private void streamWriterFlushSync()
        {
            lock (streamwriterlock)
            {
                streamWriter.Flush();
            }
        }

        // Skipping validation because of the use of test certificate
        static bool App_CertificateValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static byte[] test()
        {
            string raw = "bc 83 85 cc 1b 7f 40 55 39 0b d9 c4 08 00 45 00 " +
            "05 92 9e a6 40 00 2f 06 d8 1d 8b a2 7b 86 0a 16 " +
            "be 63 00 50 c6 72 9c ce 4b f7 8d 02 ca ee 50 10 " +
            "00 ed b0 bf 00 00 00 00 c6 01 04 00 00 00 01 88 " +
            "61 96 df 3d bf 4a 05 f5 21 ae c5 04 00 bc a0 1d " +
            "b8 d8 2e 32 d2 98 b4 6f 5f 87 49 7c a5 89 d3 4d " +
            "1f 6c 96 df 3d bf 4a 04 4a 43 5d 8a 08 01 79 40 " +
            "b7 70 2e dc 0b aa 62 d1 bf 0f 13 8c fe 5b 19 25 " +
            "75 e7 64 58 2f c8 f7 f3 52 84 8f d2 4a 8f 0f 0d " +
            "83 71 c0 b9 40 8f f2 b4 63 27 52 d5 22 d3 94 72 " +
            "16 c5 ac 4a 7f 86 02 e0 00 5b 71 af 76 86 aa 69 " +
            "d2 9a fc ff 7c 87 12 95 4d 3a 53 5f 9f 40 8b f2 " +
            "b4 b6 0e 92 ac 7a d2 63 d4 8f 89 dd 0e 8c 1a b6 " +
            "e4 c5 93 4f 40 8c f2 b7 94 21 6a ec 3a 4a 44 98 " +
            "f5 7f 8a 0f da 94 9e 42 c1 1d 07 27 5f 40 90 f2 " +
            "b1 0f 52 4b 52 56 4f aa ca b1 eb 49 8f 52 3f 85 " +
            "a8 e8 a8 d2 cb 00 19 d8 00 01 00 00 00 01 0a 3c " +
            "21 44 4f 43 54 59 50 45 20 68 74 6d 6c 3e 0a 3c " +
            "21 2d 2d 5b 69 66 20 49 45 4d 6f 62 69 6c 65 20 " +
            "37 20 5d 3e 3c 68 74 6d 6c 20 63 6c 61 73 73 3d " +
            "22 6e 6f 2d 6a 73 20 69 65 6d 37 22 3e 3c 21 5b " +
            "65 6e 64 69 66 5d 2d 2d 3e 0a 3c 21 2d 2d 5b 69 " +
            "66 20 6c 74 20 49 45 20 39 5d 3e 3c 68 74 6d 6c " +
            "20 63 6c 61 73 73 3d 22 6e 6f 2d 6a 73 20 6c 74 " +
            "65 2d 69 65 38 22 3e 3c 21 5b 65 6e 64 69 66 5d " +
            "2d 2d 3e 0a 3c 21 2d 2d 5b 69 66 20 28 67 74 20 " +
            "49 45 20 38 29 7c 28 67 74 20 49 45 4d 6f 62 69 " +
            "6c 65 20 37 29 7c 21 28 49 45 4d 6f 62 69 6c 65 " +
            "29 7c 21 28 49 45 29 5d 3e 3c 21 2d 2d 3e 3c 68 " +
            "74 6d 6c 20 63 6c 61 73 73 3d 22 6e 6f 2d 6a 73 " +
            "22 20 6c 61 6e 67 3d 22 65 6e 22 3e 3c 21 2d 2d " +
            "3c 21 5b 65 6e 64 69 66 5d 2d 2d 3e 0a 3c 68 65 " +
            "61 64 3e 0a 20 20 3c 6d 65 74 61 20 63 68 61 72 " +
            "73 65 74 3d 22 75 74 66 2d 38 22 3e 0a 20 20 3c " +
            "74 69 74 6c 65 3e 4e 67 68 74 74 70 32 3a 20 48 " +
            "54 54 50 2f 32 20 43 20 4c 69 62 72 61 72 79 20 " +
            "2d 20 6e 67 68 74 74 70 32 2e 6f 72 67 3c 2f 74 " +
            "69 74 6c 65 3e 0a 20 20 3c 6d 65 74 61 20 6e 61 " +
            "6d 65 3d 22 61 75 74 68 6f 72 22 20 63 6f 6e 74 " +
            "65 6e 74 3d 22 54 61 74 73 75 68 69 72 6f 20 54 " +
            "73 75 6a 69 6b 61 77 61 22 3e 0a 0a 20 20 0a 20 " +
            "20 3c 6d 65 74 61 20 6e 61 6d 65 3d 22 64 65 73 " +
            "63 72 69 70 74 69 6f 6e 22 20 63 6f 6e 74 65 6e " +
            "74 3d 22 4e 67 68 74 74 70 32 3a 20 48 54 54 50 " +
            "2f 32 20 43 20 4c 69 62 72 61 72 79 20 46 65 62 " +
            "20 31 36 74 68 2c 20 32 30 31 35 20 31 31 3a 31 " +
            "36 20 70 6d 20 6e 67 68 74 74 70 32 20 69 73 20 " +
            "61 6e 20 69 6d 70 6c 65 6d 65 6e 74 61 74 69 6f " +
            "6e 20 6f 66 0a 48 54 54 50 2f 32 20 61 6e 64 20 " +
            "69 74 73 20 68 65 61 64 65 72 0a 63 6f 6d 70 72 " +
            "65 73 73 69 6f 6e 20 61 6c 67 6f 72 69 74 68 6d " +
            "20 48 50 41 43 4b 20 69 6e 0a 43 2e 20 54 68 65 " +
            "20 26 68 65 6c 6c 69 70 3b 22 3e 0a 20 20 0a 0a " +
            "20 20 3c 21 2d 2d 20 68 74 74 70 3a 2f 2f 74 2e " +
            "63 6f 2f 64 4b 50 33 6f 31 65 20 2d 2d 3e 0a 20 " +
            "20 3c 6d 65 74 61 20 6e 61 6d 65 3d 22 48 61 6e " +
            "64 68 65 6c 64 46 72 69 65 6e 64 6c 79 22 20 63 " +
            "6f 6e 74 65 6e 74 3d 22 54 72 75 65 22 3e 0a 20 " +
            "20 3c 6d 65 74 61 20 6e 61 6d 65 3d 22 4d 6f 62 " +
            "69 6c 65 4f 70 74 69 6d 69 7a 65 64 22 20 63 6f " +
            "6e 74 65 6e 74 3d 22 33 32 30 22 3e 0a 20 20 3c " +
            "6d 65 74 61 20 6e 61 6d 65 3d 22 76 69 65 77 70 " +
            "6f 72 74 22 20 63 6f 6e 74 65 6e 74 3d 22 77 69 " +
            "64 74 68 3d 64 65 76 69 63 65 2d 77 69 64 74 68 " +
            "2c 20 69 6e 69 74 69 61 6c 2d 73 63 61 6c 65 3d " +
            "31 22 3e 0a 0a 20 20 0a 20 20 3c 6c 69 6e 6b 20 " +
            "72 65 6c 3d 22 63 61 6e 6f 6e 69 63 61 6c 22 20 " +
            "68 72 65 66 3d 22 2f 2f 6e 67 68 74 74 70 32 2e " +
            "6f 72 67 22 3e 0a 20 20 3c 6c 69 6e 6b 20 68 72 " +
            "65 66 3d 22 2f 66 61 76 69 63 6f 6e 2e 70 6e 67 " +
            "22 20 72 65 6c 3d 22 69 63 6f 6e 22 3e 0a 20 20 " +
            "3c 6c 69 6e 6b 20 68 72 65 66 3d 22 2f 73 74 79 " +
            "6c 65 73 68 65 65 74 73 2f 73 63 72 65 65 6e 2e " +
            "63 73 73 22 20 6d 65 64 69 61 3d 22 73 63 72 65 " +
            "65 6e 2c 20 70 72 6f 6a 65 63 74 69 6f 6e 22 20 " +
            "72 65 6c 3d 22 73 74 79 6c 65 73 68 65 65 74 22 " +
            "20 74 79 70 65 3d 22 74 65 78 74 2f 63 73 73 22 " +
            "3e 0a 20 20 3c 6c 69 6e 6b 20 68 72 65 66 3d 22 " +
            "2f 61 74 6f 6d 2e 78 6d 6c 22 20 72 65 6c 3d 22 " +
            "61 6c 74 65 72 6e 61 74 65 22 20 74 69 74 6c 65 " +
            "3d 22 6e 67 68 74 74 70 32 2e 6f 72 67 22 20 74 " +
            "79 70 65 3d 22 61 70 70 6c 69 63 61 74 69 6f 6e " +
            "2f 61 74 6f 6d 2b 78 6d 6c 22 3e 0a 20 20 3c 73 " +
            "63 72 69 70 74 20 73 72 63 3d 22 2f 6a 61 76 61 " +
            "73 63 72 69 70 74 73 2f 6d 6f 64 65 72 6e 69 7a " +
            "72 2d 32 2e 30 2e 6a 73 22 3e 3c 2f 73 63 72 69 " +
            "70 74 3e 0a 20 20 3c 73 63 72 69 70 74 20 73 72 " +
            "63 3d 22 2f 2f 61 6a 61 78 2e 67 6f 6f 67 6c 65 " +
            "61 70 69 73 2e 63 6f 6d 2f 61 6a 61 78 2f 6c 69 " +
            "62 73 2f 6a 71 75 65 72 79 2f 31 2e 39 2e 31 2f";

            string[] stringbytes = raw.Split(' ');
            byte[] bytes = new byte[stringbytes.Length];
            for (int i = 0; i < stringbytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(stringbytes[i], 16);
            }
            // for (int i = 0; i < bytes.Length; i++)
            // {
            //     Console.WriteLine(bytes[i]);
            // }
            return bytes;


        }

        internal void Close()
        {
            try
            {
                Console.WriteLine("Handle Client closing...");
                if(sslStream != null) sslStream.Dispose();
                if(streamReader != null) streamReader.Dispose();
                if(streamWriter != null) streamWriter.Dispose();
                if(binaryReader != null) binaryReader.Dispose();
                if(binaryWriter != null) binaryWriter.Dispose();
                if(streamHandler != null) streamHandler.Close();
                streamHandler = null;
                hpackEncoder = null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        bool IsPreface(byte[] data)
        {
            for (int i = 0; i < http2ConnectionPreface.Length; i++)
            {
                if (http2ConnectionPreface[i] != data[i]) return false;
            }
            return true;
        }

    }
    
    

}
