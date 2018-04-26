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
            //Server starts listening to port, and responding to webpage.
            server.Listen(443);
        }
    }
}
