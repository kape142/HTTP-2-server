using System;
using System.Security.Cryptography.X509Certificates;
using lib;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //Creating the certificate
            var serverCertificate = new X509Certificate2("Certificate/TcpTLSServer_TemporaryKey.pfx", "1234");
            //Creating the server
            Server server = new Server(serverCertificate); // serverCertificate);

            //test Get method
            server.Get("testurl", (req, res) => {
                Console.WriteLine("GET TEST URLVIRKER");
                res.Send("get fra test url");
            });

            //test Post method
            server.Post("testurl", (req, res) => {
                Console.WriteLine("POST TEST URLVIRKER");
                res.Send("post fra test url");
            });

            //Server starts listening to port, and responding to webpage.
            server.Listen(443);

            
        }
    }
}
