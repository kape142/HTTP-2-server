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

namespace lib
{
    class HandleClient
    {
        TcpClient tcpClient;
        SslStream sslStream;
        StreamReader streamReader;
        StreamWriter streamWriter;
        BinaryReader binaryReader;
        BinaryWriter binaryWriter;
        private object streamreaderlock = new object();
        private object streamwriterlock = new object();
        bool HttpUpgraded = false;

        public void StartThreadForClient(TcpClient tcpClient, int port, X509Certificate2 certificate = null)
        {
            this.tcpClient = tcpClient;
            try
            {
                if(port == Server.HTTPS_PORT)
                {
                    sslStream = new SslStream(tcpClient.GetStream(), false, App_CertificateValidation);
                    sslStream.AuthenticateAsServer(certificate, false, SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
                    streamReader = new StreamReader(sslStream);
                    streamWriter = new StreamWriter(sslStream);
                    binaryReader = new BinaryReader(sslStream);
                    binaryWriter = new BinaryWriter(sslStream);
                }
                else
                {
                    streamReader = new StreamReader(tcpClient.GetStream());
                    streamWriter = new StreamWriter(tcpClient.GetStream());
                    binaryReader = new BinaryReader(tcpClient.GetStream());
                    binaryWriter = new BinaryWriter(tcpClient.GetStream());
                }
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
            while (true)
            {
                if (!HttpUpgraded)
                {
                    await Task.Run(() => ReadStreamToString((msg) => {
                        // a request has ben recived
                        HTTP1Request req = new HTTP1Request(msg);
                        Console.WriteLine(req.ToString());
                        Response res = Response.From(req);
                        Console.WriteLine(res.ToString());
                        Task.Run(() => WriteResponse(res));
                        // todo vent på preface
                        if (req.IsUpgradeTo2)
                        {
                            HttpUpgraded = true;
                            Task.Run(() =>
                            {
                                // start sending file

                            });

                            Task.Run(() => {
                                //HTTP2Frame connectionpreface = new HTTP2Frame(0).addSettingsPayload(HTTP2Frame.ACK, new Tuple<byte, int>[0]);
                                //WriteFrame(connectionpreface);
                                Tuple<short, int>[] settings = new Tuple<short, int>[]
                                {
                                    Tuple.Create(HTTP2Frame.SETTINGS_MAX_CONCURRENT_STREAMS, 100),
                                    Tuple.Create(HTTP2Frame.SETTINGS_INITIAL_WINDOW_SIZE, Server.MAX_HTTP2_FRAME_SIZE)
                                };
                                HTTP2Frame firstSettingsframe = new HTTP2Frame(0).addSettingsPayload(settings);
                                Console.WriteLine(firstSettingsframe);
                                WriteFrame(firstSettingsframe);
                            });
                        }
                    }, () => {
                        Thread.Sleep(100);
                    }));
                }
                else
                {
                    await Task.Run(() => ReadStreamToFrameBytes((framedata) => {
                        HTTP2Frame frame = new HTTP2Frame(framedata);
                        Console.WriteLine(frame.ToString());

                    }, () => {
                        Thread.Sleep(100);
                    }));
                }
                
            }
        }

        private async Task RespondToHTTP2Get(string url)
        {

        }

        private async Task ReadStreamToString(Action<string> onRequest, Action onNoRequest)
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

        private async Task ReadStreamToFrameBytes(Action<byte[]> framedata, Action onEmptyFrame)
        {
            NetworkStream r = tcpClient.GetStream();
            byte[] myReadBuffer = new byte[3];
            int numberOfBytesRead = 0;

            // Incoming message may be larger than the buffer size.
            while (r.DataAvailable)
            {
                numberOfBytesRead = await r.ReadAsync(myReadBuffer, 0, myReadBuffer.Length);
            }
            if(myReadBuffer != null && myReadBuffer.Length == 3 && myReadBuffer[0] != 0 && myReadBuffer[1] != 0 && myReadBuffer[2] != 0)
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
                Console.WriteLine("Lengt of recived frame: " + length);
                byte[] data = new byte[length];
                data[0] = source[0];
                data[1] = source[1];
                data[2] = source[2];

                await r.ReadAsync(data, 3, length - 3);

                for (int i = 0; i < length; i++)
                {
                    Console.Write(data[i] + " ");
                }
                Console.WriteLine();

                framedata(data);
            }
            else
            {
                onEmptyFrame();
            }
            
        }

        private async Task<string> streamReaderReadLineSync()
        {
            lock (streamreaderlock)
            {
                return streamReader.ReadLine();
            }
        }



        private async Task WriteFrame(HTTP2Frame frame)
        {
            binaryWriter.Flush();
            binaryWriter.Write(frame.getBytes(), 0, frame.getBytes().Length);
        }
        
        private async Task WriteResponse(Response r)
        {
            if (r == null) return;
            streamWriterFlushSync();
            streamWriterWriteSync(r.ToString());
            // streamWriter.Flush();
            if (r.Data == null) return;
            streamWriterWriteSync(r.Data, 0, r.Data.Length);
            streamWriter.Flush();
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
    }
    
    

}
