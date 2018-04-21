using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using lib.HTTPObjects;

namespace lib
{
    public class Server
    {
        internal const int HTTPS_PORT = 443;
        internal const string HTTP1V = "HTTP/1.1";
        internal const string SWITCHING_PROTOCOLS = "101 Switching Protocols";
        internal const string OK = "200 OK";
        internal const string NO_CONTENT = "204 No Content";
        internal const string ERROR = "400 Bad Request";
        internal const string SERVER = "prosjekthttp2";
        internal const string DIR = "WebApp";
        internal const int MAX_HTTP2_FRAME_SIZE = 16384;
        private string IpAddress;
        internal static Http2.Hpack.Encoder hPackEncoder = new Http2.Hpack.Encoder();
        internal static int Port { get; private set; }
        private X509Certificate2 Certificate;
        internal static Dictionary<string, Action<HTTP1Request, Response>> registerdActionsOnUrls;


        /*
        Dictionary<string, Func<HTTPRequest, HTTPResponse>> restLibrary = new Dictionary<string, Action<HTTPRequest, HTTPResponse>>();

        public void Get(string path, Action<HTTPRequest, HTTPResponse> callback)
        {
            restLibrary.Add("GET/"+path, callback);
        }

        public void Post(string path, Action<HTTPRequest, HTTPResponse> callback)
        {
            restLibrary.Add("POST/" + path, callback);
        }
        */

        public Server(string ipAddress, X509Certificate2 certificate = null)
        {
            registerdActionsOnUrls = new Dictionary<string, Action<HTTP1Request, Response>>();
            IpAddress = ipAddress;
            Certificate = certificate;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        public void Listen(int port)
        {
            Port = port;
            TcpListener tcpListener = null;
            try
            {
                Console.WriteLine($"Server is starting at {IpAddress} on {Port}");
                IPAddress localAddr = IPAddress.Parse(IpAddress);
                tcpListener = new TcpListener(localAddr, Port);
                tcpListener.Start();
                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    HandleClient handleClient = new HandleClient();
                    handleClient.StartThreadForClient(tcpClient, Port, Certificate);
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                Console.ReadLine();
            }
            finally
            {
                if (tcpListener != null)
                {
                    Console.WriteLine("Listener stopping");
                    tcpListener.Stop();
                }

            }
        }

        public void Get(string url, Action<HTTP1Request, Response> action)
        {
            registerdActionsOnUrls.Add(url, action);
        }

        public static void testFrame(){
            var fc = new HTTP2Frame(128).AddHeaderPayload(new byte[6], 16,0x8,true, 0x2, true, false);
            byte[] bytes = fc.getBytes();
            foreach(byte b in bytes)
                Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));

            /*Console.WriteLine(fc.ToString());
            fc.addSettingsPayload(new Tuple<short, int>[] {new Tuple<short,int>(HTTP2Frame.SETTINGS_MAX_FRAME_SIZE,128) });
            var by = fc.getBytes();
            foreach (byte b in by)
                Console.Write($"{b} ");
            Console.WriteLine();
            Console.WriteLine(fc.ToString());*/
        }
    }
}
