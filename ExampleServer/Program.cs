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
            Server server = new Server("10.24.91.159", serverCertificate); // serverCertificate);

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
            server.Listen(443);

            /*
            //Zip test
            DirectoryInfo di = new DirectoryInfo(@"C:\Users\Martin Wangen\source\repos\HTTP-2-server\ExampleServer\WebApp\");
            FileInfo fi = new FileInfo(@"C:\Users\Martin Wangen\source\repos\HTTP-2-server\ExampleServer\WebApp\temp\index.html.gz");
            ZipStream.Compress(di);
            ZipStream.Decompress(fi);


            Console.ReadLine();
            */

            //Server.testFrame();
            /*
            ThreadTest t = new ThreadTest();
            t.start();
            Console.ReadLine();
            t.alive = false;
            Console.ReadLine();
            */
        }

        /*
        class ThreadTest
        {
            public bool alive { get; set; } = true;
            int rounds = 0;

            public void start()
            {
                Thread t = new Thread(Run);
                t.Start();
            }
            private void Run()
            {
                while (alive)
                {
                    Thread.Sleep(5000);
                    Console.WriteLine("Running");
                }
            }
        }
        */
    }
}
