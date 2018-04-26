using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using lib;
using lib.HTTPObjects;

namespace ExampleServer
{
    class Program
    {
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

        static void Main(string[] args)
        {
            
            //lib.HandleClient.test();
            var serverCertificate = new X509Certificate2("Certificate/TcpTLSServer_TemporaryKey.pfx", "1234");
            //Server server = new Server("10.22.190.99", null);
            Server server = new Server(GetLocalIPAddress(), serverCertificate); // serverCertificate);

            
            server.Get("testurl", (req, res) => {
                Console.WriteLine("GET TEST URLVIRKER");
                res.Send("get fra test url");
            });

            server.Post("testurl", (req, res) => {
                Console.WriteLine("POST TEST URLVIRKER");
                res.Send("post fra test url");
            });

            server.Get("jsonobject", (req, res) =>
             {
                 res.Send("{ \"name\":\"Jone\", \"age\":39, \"car\":null }");
             });
            /*
            server.Get("/testurl", (req, res) =>
            {
                Console.WriteLine("testurl virker");
                byte[] mottat = (byte[])req;
                byte[] b = new byte[2];
                b[0] = (byte)'H';
                b[1] = (byte)'E';
                res = (byte[])b;
            });
            */
            server.Listen(443);
        }
    }
}
