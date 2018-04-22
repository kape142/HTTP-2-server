using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using lib;
using lib.HTTPObjects;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //var serverCertificate = new X509Certificate2("Certificate/TcpTLSServer_TemporaryKey.pfx", "1234");
            //Server server = new Server("10.22.190.99", null);
            //Server server = new Server("10.0.0.142", serverCertificate);

            /*
            server.Get("/testurl", (req, res) =>
            {
                Console.WriteLine("testurl virker");
                char[] b = new char[2];
                b[0] = 'H';
                b[1] = 'E';
                res.Send(b);
            });
            */
            //server.Listen(80);

            /*
            //Zip test
            DirectoryInfo di = new DirectoryInfo(@"C:\Users\Martin Wangen\source\repos\HTTP-2-server\ExampleServer\WebApp\");
            FileInfo fi = new FileInfo(@"C:\Users\Martin Wangen\source\repos\HTTP-2-server\ExampleServer\WebApp\temp\index.html.gz");
            ZipStream.Compress(di);
            ZipStream.Decompress(fi);


            Console.ReadLine();
            */

            int i = 1823423647;
            var b = BitConverter.GetBytes(i);
            Array.Reverse(b);
            foreach (byte by in b)
                Console.Write($"{by} ");
            Console.WriteLine(Bytes.ConvertFromIncompleteByteArray(b));
        }
    }
}
