using System;
using System.IO;
using lib;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server("127.0.0.1");

            /*
            //Zip test
            DirectoryInfo di = new DirectoryInfo(@"C:\Users\Martin Wangen\source\repos\HTTP-2-server\ExampleServer\WebApp\");
            FileInfo fi = new FileInfo(@"C:\Users\Martin Wangen\source\repos\HTTP-2-server\ExampleServer\WebApp\temp\index.html.gz");
            ZipStream.Compress(di);
            ZipStream.Decompress(fi);


            Console.ReadLine();
            */
        }
    }
}
