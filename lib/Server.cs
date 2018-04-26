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
        
        internal const string DIR = "WebApp";
        internal const int MAX_HTTP2_FRAME_SIZE = 16384;

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
            registerdActionsOnUrls = new Dictionary<string, Action<object, object>>();
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
                    handleClient.StartThreadForClient(tcpClient, Port, Certificate);
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

        public void Get(string url, RestURI.HTTPMethod action)
        {
            RestURI.RestLibrary.AddURI("GET", url, action);
        }

        public void Post(string url, RestURI.HTTPMethod action)
        {
            RestURI.RestLibrary.AddURI("POST", url, action);
        }

        //private void Clean()
        //{
        //    while (cleanupThreadRunning)
        //    {
        //        var disconnectedClients = clients.FindAll(x => !x.Connected);
        //        disconnectedClients.ForEach(y => y.Close());
        //        clients.RemoveAll(u => !u.Connected);
        //        Thread.Sleep(5000);
        //    }
        //}
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


        public static void testFrame(){
            var fc = new HTTP2Frame(128).AddHeaderPayload(new byte[6], 16,0x8,true, 0x2, true, false);
            byte[] bytes = fc.GetBytes();
            foreach(byte b in bytes)
                Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));

            /*Console.WriteLine(fc.ToString());
            fc.AddSettingsPayload(new Tuple<short, int>[] {new Tuple<short,int>(HTTP2Frame.MaxFrameSize,128) });
            var by = fc.GetBytes();
            foreach (byte b in by)
                Console.Write($"{b} ");
            Console.WriteLine();
            Console.WriteLine(fc.ToString());*/
        }

        public static void testRestURI()
        {
            RestURI.RestLibrary.AddURI("GET", "shoppinglists/favourite/:householdid/username/shoppinglistid",(req,res) => {
                res.Send($"HouseholdID: {req.Params["householdid"]}, username: {req.Params["username"]}, shoppinglistid: {req.Params["shoppinglistid"]}");
            });
            RestURI.RestLibrary.AddURI("GET", "shoppinglists/favourite/:householdid/", (req, res) => Console.Write("2"));
        }
    }
}
