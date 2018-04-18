using System;
using HTTP2Server;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            HTTP2Server.Server.test();
        }
    }
}
