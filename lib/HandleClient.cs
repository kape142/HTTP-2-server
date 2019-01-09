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
        private static int _nrOfClientsMade = 0;
        private int _currentStreamIdPromise = 0;
        private object _streamreaderlock = new object();
        private TcpClient _tcpClient;
        private SslStream _sslStream;
        private StreamReader _http1Reader;
        private StreamWriter _http1Writer;
        private NetworkStream _http2Reader;
        private NetworkStream _http2Writer;
        private SslStream _sslReader;
        private SslStream _sslWriter;
        internal Settings settings = Settings.Default;
        private object _streamwriterlock = new object();
        private object _binaryreaderlock = new object();
        private object _binarywriterlock = new object();
        private byte[] _http2ConnectionPreface = { 0x50, 0x52, 0x49, 0x20, 0x2a, 0x20, 0x48, 0x54, 0x54, 0x50, 0x2f, 0x32, 0x2e, 0x30, 0x0d, 0x0a, 0x0d, 0x0a, 0x53, 0x4d, 0x0d, 0x0a, 0x0d, 0x0a };
        private bool _HttpUpgraded = false;
        private StreamHandler _streamHandler;
        private bool _useSsl = false;
        internal uint windowSize = Settings.Default.InitialWindowSize;
        private Task _taskRead;

        public HandleClient()
        {
            Console.WriteLine("Handleclient made: " + ++_nrOfClientsMade);
            
        }

        public bool Connected { get; private set; } = true;
        public int ClientPort { get; private set; }
        public Http2.Hpack.Encoder hpackEncoder { get; private set; }
        public Http2.Hpack.Decoder hpackDecoder { get; private set; }
        public int NextStreamId {
            get
            {
                _currentStreamIdPromise += _currentStreamIdPromise + 2;
                return _currentStreamIdPromise; // todo thread safe
            }
        }

        public async void StartTaskForClient(TcpClient tcpClient, int port, X509Certificate2 certificate = null)
        {
            this._tcpClient = tcpClient;
            if (certificate != null) _useSsl = true;
            try
            {
                if(_useSsl)
                {
                    _sslStream = new SslStream(tcpClient.GetStream(), false, App_CertificateValidation);
                    _sslStream.ReadTimeout = 10000;
                    //_sslStream.WriteTimeout = 100;
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

                    await _sslStream.AuthenticateAsServerAsync(options, CancellationToken.None);
                    if (_sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2) InitUpgradeToHttp2();
                    _http1Reader = new StreamReader(_sslStream);
                    _http1Writer = new StreamWriter(_sslStream);
                    _sslReader = _sslStream; 
                    _sslWriter = _sslStream;
                    Console.WriteLine("New client connected with TLS 1.2---------------------------------");
                    Console.WriteLine($"SslStreamInfo:\nCanTimeout: {_sslStream.CanTimeout} ReadTimeout: {_sslStream.ReadTimeout} WriteTimout: {_sslStream.ReadTimeout}");
                }
                else
                {
                    _http1Reader = new StreamReader(tcpClient.GetStream());
                    _http1Writer = new StreamWriter(tcpClient.GetStream());
                    _http2Reader = tcpClient.GetStream();
                    _http2Writer = tcpClient.GetStream();
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

            _taskRead = Task.Factory.StartNew(() => StartReadingAsync());
        }

        internal void Close()
        {
            try
            {
                if (!Connected)
                    return;
                Console.WriteLine("Handle Client closing...");
                if(_sslStream != null) _sslStream.Dispose();
                if(_http1Reader != null) _http1Reader.Dispose();
                if(_http1Writer != null) _http1Writer.Dispose();
                if(_sslReader != null) _sslReader.Dispose();
                if(_sslWriter != null) _sslWriter.Dispose();
                if(_streamHandler != null) _streamHandler.Close();
                _streamHandler = null;
                hpackEncoder = null;
                _tcpClient.Close();
                Connected = false;
            }
            catch (Exception)
            {
                throw;
            }
        }


        private void InitUpgradeToHttp2()
        {
            hpackEncoder = new Http2.Hpack.Encoder(new Http2.Hpack.Encoder.Options {
                DynamicTableSize = 0,
                HuffmanStrategy = Http2.Hpack.HuffmanStrategy.Never,
            });
            hpackDecoder = new Http2.Hpack.Decoder();
            _streamHandler = new StreamHandler(this);
            _streamHandler.StartSendThread();
            _HttpUpgraded = true;
        }
       
        private bool IsPreface(byte[] data, int length = 24)
        {
            for (int i = 0; i < length; i++)
            {
                if (_http2ConnectionPreface[i] != data[i]) return false;
            }
            return true;
        }

        private async Task StartReadingAsync()
        {
            while (Connected)
            {
                try
                {
                    if (!_HttpUpgraded)
                    {
                        await ReadStreamToString();
                    }
                    else
                    {
                        await ReadStreamToFrameBytes();
                    }
                    if (!_tcpClient.Connected)
                    {
                        Console.WriteLine($"TcpClient disconnected from {ClientPort}");
                        break;
                    }
                }
                catch (InvalidOperationException ioex)
                {
                    Console.WriteLine("StartReadingAsync()\nInvalidOperationException\n" + ioex);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("StartReadingAsync()\n" + ex);
                }
            }
            Close();
        }

        private async Task ReadStreamToString()
        {
            try
            {
                string msg = "";
                while (_http1Reader.Peek() != -1)
                {
                    msg += await _http1Reader.ReadLineAsync() + "\n";
                }
                if(msg.Length > 5)
                {
                    // a request has ben received
                    HTTP1Request req = new HTTP1Request(msg);
                    Console.WriteLine(req.ToString());
                    HTTP1Response res = HTTP1Response.From(req);
                    Console.WriteLine(res.ToString());
                    await WriteResponseAsync(res);
                    // todo vent på preface
                    if (req.IsUpgradeTo2)
                    {
                        InitUpgradeToHttp2();
                        _streamHandler.SendFrame(new HTTP2Frame(0).AddSettingsPayload(new (ushort, uint)[0])); // connection preface
                                                                                                               //_streamHandler.RespondWithFirstHTTP2(req.HttpUrl);
                    }
                }
                else
                {
                    await Task.Delay(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadStreamToString()\n" + ex);
            }
        }

        private async Task ReadStreamToFrameBytes()
        {
            byte[] myReadBuffer = new byte[3];
            int numberOfBytesRead = 0;
            try // HACK
            {
                if(_useSsl) numberOfBytesRead = await _sslReader.ReadAsync(myReadBuffer, 0, myReadBuffer.Length);
                else numberOfBytesRead = await _http2Reader.ReadAsync(myReadBuffer, 0, myReadBuffer.Length);
            } catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            if (numberOfBytesRead == 3)
            {
                int length = 0;
                if (IsPreface(myReadBuffer, 3))
                    length = 24;
                else
                {
                    length = Bytes.ConvertFromIncompleteByteArray(myReadBuffer) + 9;
                }
                byte[] framedata = new byte[length];
                framedata[0] = myReadBuffer[0];
                framedata[1] = myReadBuffer[1];
                framedata[2] = myReadBuffer[2];
                if(_useSsl) await _sslReader.ReadAsync(framedata, 3, length - 3);
                else await _http2Reader.ReadAsync(framedata, 3, length - 3);
                StringBuilder s = new StringBuilder();
                s.AppendLine("-----------------------");
                foreach (byte b in framedata)
                    s.Append($"{b} ");

                // preface or frame
                if (IsPreface(framedata))
                {
                    Console.WriteLine("Connectionpreface received");
                    _streamHandler.SendFrame(new HTTP2Frame(0).AddSettingsPayload(new (ushort, uint)[0], false));
                    if (framedata.Length > _http2ConnectionPreface.Length)
                        framedata = Bytes.GetPartOfByteArray(_http2ConnectionPreface.Length, framedata.Length, framedata);
                    else
                    {
                        Console.WriteLine(s);
                        return;
                    }
                }
                HTTP2Frame frame = new HTTP2Frame(framedata);
                if (_streamHandler.IncomingStreamExist(frame.StreamIdentifier))
                {
                    _streamHandler.AddIncomingFrame(frame);
                }
                else
                {
                    _streamHandler.AddStreamToIncomming(new HTTP2Stream((uint)frame.StreamIdentifier, StreamState.Open, WindowSize: settings.InitialWindowSize));
                    _streamHandler.AddIncomingFrame(frame);
                }
                s.AppendLine("\n-----------------------");
                Console.WriteLine(s);
            }
            else
            {
                await Task.Delay(10);
            }
            
        }

        private async Task WriteResponseAsync(HTTP1Response r)
        {
            try
            {
                if (r == null) return;
                await _http1Writer.FlushAsync(); //streamWriterFlushSync();
                await _http1Writer.WriteAsync(r.ToString()); //streamWriterWriteSync(r.ToString());
                if (r.Data == null) return;
                await _http1Writer.WriteAsync(r.Data, 0, r.Data.Length); //streamWriterWriteSync(r.Data, 0, r.Data.Length);
                await _http1Writer.FlushAsync(); //streamWriterFlushSync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("WriteResponse()\n" + ex);
            }
        }
        
        internal async Task WriteFrameAsync(HTTP2Frame frame)
        {
            try
            {
                StringBuilder s = new StringBuilder();
                s.AppendLine("---------------");
                s.AppendLine("Send frame: ");
                s.AppendLine(frame.ToString());
                s.AppendLine("---------------");
                Console.WriteLine(s);
                if (_useSsl)
                {
                    await _sslWriter.FlushAsync();
                    await _sslWriter.WriteAsync(frame.GetBytes(), 0, frame.GetBytes().Length);
                }
                else
                {
                    await _http2Writer.FlushAsync();
                    await _http2Writer.WriteAsync(frame.GetBytes(), 0, frame.GetBytes().Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("WriteFrame()\n" + ex);
            }
        }

        // Skipping validation because of the use of test certificate
        private static bool App_CertificateValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
