using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace lib
{
    public class Server
    {
        public const int HTTP_PORT = 80;
        public const int HTTPS_PORT = 443;
        public const string HTTP1V = "HTTP/1.1";
        public const string SWITCHING_PROTOCOLS = "101 Switching Protocols";
        public const string OK = "200 OK";
        public const string NO_CONTENT = "204 No Content";
        public const string ERROR = "400 Bad Request";
        public const string SERVER = "prosjekthttp2";
        public const string DIR = "WebApp";
        public const int MAX_HTTP2_FRAME_SIZE = 16384;
        private string IpAddress;
        public static int Port { get; private set; }
        private X509Certificate2 Certificate;

        public Server(string ipAddress, X509Certificate2 certificate = null)
        {
            IpAddress = ipAddress;
            Port = (certificate == null) ? HTTP_PORT : HTTPS_PORT;
            Certificate = certificate;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            TcpListener tcpListener = null;
            try
            {
                Console.WriteLine($"Server is starting at {IpAddress} on {Port}");
                IPAddress localAddr = IPAddress.Parse(ipAddress);
                tcpListener = new TcpListener(localAddr, Port);
                tcpListener.Start();
                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    HandleClient handleClient = new HandleClient();
                    handleClient.StartThreadForClient(tcpClient, certificate);
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                Console.ReadLine();
            }
            finally
            {
                if (tcpListener != null)
                {
                    Console.WriteLine("Listener stopping");
                    tcpListener.Stop();
                }

            }
        }
    }
}
