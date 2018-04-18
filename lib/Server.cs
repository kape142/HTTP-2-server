using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace lib
{
    public class Server
    {
        public const int PORT = 443;
        public const string HTTP1V = "HTTP/1.1";
        public const string SWITCHING_PROTOCOLS = "101 Switching Protocols";
        public const string OK = "200 OK";
        public const string NO_CONTENT = "204 No Content";
        public const string ERROR = "400 Bad Request";
        public const string SERVER = "prosjekthttp2";
        public const string DIR = "WebApp";
        private string IpAddress;
        private X509Certificate2 Certificate;

        public Server(string ipAddress, X509Certificate2 certificate)
        {
            IpAddress = ipAddress;
            Certificate = certificate;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            TcpListener tcpListener = null;
            try
            {
                Console.WriteLine("Server is starting...");
                IPAddress localAddr = IPAddress.Parse(ipAddress);
                tcpListener = new TcpListener(localAddr, PORT);
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
