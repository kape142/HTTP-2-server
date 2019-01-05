using lib;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //Creating the certificate
            var serverCertificate = new X509Certificate2("Certificate/TcpTLSServer_TemporaryKey.pfx", "1234");
            //Creating the server
            Server server;
            server = new Server(serverCertificate);
            
            server.Use("WebApp");

            server.Listen(60000);
            
        }
    }
}
