using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using lib;
using lib.HTTPObjects;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //lib.HandleClient.test();
            var serverCertificate = new X509Certificate2("Certificate/TcpTLSServer_TemporaryKey.pfx", "1234");
            //Server server = new Server("10.22.190.99", null);
            Server server = new Server( serverCertificate); //, serverCertificate); // serverCertificate);

            server.Listen(443);
        }
    }
}
