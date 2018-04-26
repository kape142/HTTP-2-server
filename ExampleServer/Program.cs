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
            Server server = new Server(GetLocalIPAddress(), serverCertificate); // serverCertificate);

            
            server.Get("testurl", (req, res) => {
                Console.WriteLine("GET TEST URLVIRKER");
                res.Send("get fra test url");
            });

            server.Post("testurl", (req, res) => {
                Console.WriteLine("POST TEST URLVIRKER");
                res.Send("post fra test url");
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
            //Creating the server
            Server server = new Server(serverCertificate); // serverCertificate);
            //Server starts listening to port, and responding to webpage.
            server.Listen(443);
        }
    }
}
