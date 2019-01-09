using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using lib.HTTPObjects;
using System.Threading;

namespace lib
{
    public class Server
    {
        internal const int HTTPS_PORT = 443;
        
        internal const int MAX_HTTP2_FRAME_SIZE = 16384;

        internal static string DIR = "WebApp";
        public static bool UseGZip { get; set; } = true;
        public static bool UseDebugDirectory { get; set; } = false;
        internal static string IpAddress;
        internal static int Port { get; private set; }
        private X509Certificate2 Certificate;
        private List<HandleClient> clients = new List<HandleClient>();

        public Server(string ipAddress, X509Certificate2 certificate = null)
        {
            IpAddress = ipAddress;
            Certificate = certificate;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        public Server(X509Certificate2 certificate = null)
        {
            IpAddress = GetLocalIPAddress();
            Certificate = certificate;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }


        public void Listen(int port)
        {
            Port = port;
            TcpListener tcpListener = null;
            try
            {
                Console.WriteLine($"Server is starting at {IpAddress} on {Port}");
                IPAddress localAddr = IPAddress.Parse(IpAddress);
                tcpListener = new TcpListener(localAddr, Port);
                tcpListener.Start();
                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    HandleClient handleClient = new HandleClient();
                    handleClient.StartTaskForClient(tcpClient, Port, Certificate);
                    clients.Add(handleClient);
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

        public void Get(string uri, RestURI.HTTPMethod action)
        {
            RestURI.RestLibrary.AddURI("GET", uri, action);
        }

        public void Post(string uri, RestURI.HTTPMethod action)
        {
            RestURI.RestLibrary.AddURI("POST", uri, action);
        }

        public void Use(string path)
        {
            DIR = path;
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
