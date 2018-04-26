using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lib
{
    public class HTTP1Response
    {
        private const string HTTP1V = "HTTP/1.1";
        private const string RESPONSE_SWITCHING_PROTOCOLS = "101 Switching Protocols";
        private const string RESPONSE_OK = "200 OK";
        private const string RESPONSE_NO_CONTENT = "204 No Content";
        private const string RESPONSE_BAD_REQUEST = "400 Bad Request";
        private const string RESPONSE_NOT_FOUND = "404 Not Found";
        private const string RESPONSE_SERVER = "prosjekthttp2";

        private HTTP1Response(string httpv, string status, IDictionary<string, string> headerlines, char[] data)
        {
            HTTPv = httpv;
            Status = status;
            HeaderLines = headerlines;
            Data = data;

        }

        public string HTTPv { get; private set; }
        public string Status { get; private set; }
        public IDictionary<string, string> HeaderLines { get; private set; }
        public char[] Data { get; private set; }

        public void Send(char[] data)
        {
            this.Data = data;
        }
        public override string ToString()
        {
            string ret = HTTPv + " " + Status + "\r\n";
            if (HeaderLines != null)
            {
                foreach (var item in HeaderLines)
                {
                    ret += item.Key + " : " + item.Value + "\r\n";
                }
            }
            return ret + "\r\n"; ;
        }
        internal static HTTP1Response From(HTTP1Request req)
        {
            if (req == null)
            {
                return NullRequest();
            }
            switch (req.Httpv)
            {
                case "HTTP/1.0":
                case "HTTP/1.1":
                    if (req.IsUpgradeTo2)
                    {
                        return DoUppgrade(req);
                    }
                    switch (req.Type)
                    {
                        case "HEAD":
                            return GetHTTP1Response(req, true);
                        case "GET":
                            return GetHTTP1Response(req);
                        default:
                            Console.WriteLine("Method not allowed: " + req.Type);
                            return new HTTP1Response(HTTP1V, "405 Method Not Allowed", null, new char[0]);
                    }
                case "HTTP/2.0":
                    return null;
                default:
                    Console.WriteLine("HTTP version not supported: " + req.Httpv);
                    return new HTTP1Response(HTTP1V, "405 Method Not Allowed", null, new char[0]);
            }
        }
        private static HTTP1Response GetHTTP1Response(HTTP1Request req, bool headrequest=false)
        {
            Console.WriteLine("Responding with http/1.1...");
            Dictionary<string, string> lst = new Dictionary<string, string>();
            lst.Add("Server", RESPONSE_SERVER);
            if (headrequest) return new HTTP1Response(HTTP1V, RESPONSE_NO_CONTENT, lst, new char[0]);
            String file = null;
            if (req.HttpUrl == "")
            {
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\index.html";
            }
            else
            {
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\" + req.HttpUrl;
            }
            FileInfo fi = new FileInfo(file);
            if (fi.Exists)
            {
                using(FileStream fs = fi.OpenRead())
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        char[] d = new char[fs.Length];
                        reader.Read(d, 0, d.Length);
                        lst.Add("Content-Type", Mapping.MimeMap[fi.Extension]);
                        lst.Add("Accept-Ranges", "bytes");
                        lst.Add("Content-Length", d.Length.ToString());
                        lst.Add("Keep-Alive", "timeout=5, max=100");
                        lst.Add("Connection", "Keep-Alive");
                        return new HTTP1Response(HTTP1V, RESPONSE_OK, lst, (headrequest)? new char[0] : d);
                    }
                }
            }
            else
            {
                Console.WriteLine("File not found" + fi.FullName);
                return NotFound();
            }
        }
        private static HTTP1Response DoUppgrade(HTTP1Request req)
        {
            string value = req.HeaderLines["Upgrade"];
            Console.WriteLine("Client is requesting an upgrade");
            if (value.Equals("h2"))
            {
                // TLS 
                if (Server.Port != Server.HTTPS_PORT) throw new Exception("H2 requested on a non secure stream"); // todo
                Console.WriteLine("h2 requested");
                return GetResponseForUpgradeToh2();

            }
            else if (value.Equals("h2c"))
            {
                Console.WriteLine("h2c requested");
                return GetResponseForUpgradeToh2c();
            }
            else
            {
                Console.WriteLine("Unknown upgrade requested: " + value);
                return NullRequest();
            }
        }
        private static HTTP1Response NullRequest()
        {
            return new HTTP1Response(HTTP1V, RESPONSE_BAD_REQUEST, null, new char[0]);
        }
        private static HTTP1Response NotFound()
        {
            return new HTTP1Response(HTTP1V, RESPONSE_NOT_FOUND, null, new char[0]);
        }
        private static HTTP1Response GetResponseForUpgradeToh2c()
        {
            Dictionary<string, string> lst = new Dictionary<string, string>
            {
                { "Connection", "Upgrade" },
                { "Upgrade", "h2c" }
            };
            char[] d = new char[0];
            return new HTTP1Response(HTTP1V, RESPONSE_SWITCHING_PROTOCOLS, lst, d);
        }
        private static HTTP1Response GetResponseForUpgradeToh2()
        {
            Dictionary<string, string> lst = new Dictionary<string, string>
            {
                { "Connection", "Upgrade" },
                { "Upgrade", "h2" }
            };
            char[] d = new char[0];
            return new HTTP1Response(HTTP1V, RESPONSE_SWITCHING_PROTOCOLS, lst, d);
        }
    }
}
