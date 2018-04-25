using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lib
{
    public class HTTP1Response
    {
        public string HTTPv { get; private set; }
        public string Status { get; private set; }
        public IDictionary<string, string> HeaderLines { get; private set; }
        public char[] Data { get; private set; }


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
                    if (Server.registerdActionsOnUrls.ContainsKey("/" + req.HttpUrl))
                    {
                        Action<HTTP1Request, HTTP1Response> a = Server.registerdActionsOnUrls["/"+ req.HttpUrl];
                        HTTP1Response res = new HTTP1Response(req.Httpv, Server.OK, null, null);
                        a(req, res);
                        return res;
                    }
                    switch (req.Type)
                    {
                        case "HEAD":
                            return HTTP1Response(req, true);
                        case "GET":
                            return HTTP1Response(req);
                        default:
                            Console.WriteLine("Method not allowed: " + req.Type);
                            return new HTTP1Response(Server.HTTP1V, "405 Method Not Allowed", null, new char[0]);
                    }
                case "HTTP/2.0":
                    return null;
                default:
                    Console.WriteLine("HTTP version not supported: " + req.Httpv);
                    return new HTTP1Response(Server.HTTP1V, "405 Method Not Allowed", null, new char[0]);
            }
            #region old
            /*
            if (req.Type == "GET")
            {
                if (req.HeaderLines.ContainsKey("Upgrade"))
                {
                    string value = req.HeaderLines["Upgrade"];
                    Console.WriteLine("Client is requesting an upgrade");
                    if (value.Equals("h2"))
                    {
                        Console.WriteLine("h2 requested");
                        return UpgradeToh2();

                    }
                    else if (value.Equals("h2c"))
                    {
                        Console.WriteLine("h2c requested");
                        return UpgradeToh2c();
                    }
                    else
                    {
                        Console.WriteLine("Unknown upgrade requested: " + value);
                        return NullRequest();
                    }
                }
                else
                {
                    Console.WriteLine("Responding with http/1.1...");
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
                        FileStream fs = fi.OpenRead();
                        BinaryReader reader = new BinaryReader(fs);
                        char[] d = new char[fs.Length];
                        reader.Read(d, 0, d.Length);
                        Dictionary<string, string> lst = new Dictionary<string, string>();
                        lst.Add("Server", Server.SERVER);
                        lst.Add("Content-Type", Mapping.MIME_MAP[fi.Extension]);
                        lst.Add("Accept-Ranges", "bytes");
                        lst.Add("Content-Length", d.Length.ToString());
                        lst.Add("Keep-Alive", "timeout=5, max=100");
                        lst.Add("Connection", "Keep-Alive");
                        return new Response(Server.HTTP1V, Server.OK, lst, d);
                    }
                    else
                    {
                        Console.WriteLine("File not found" + fi.FullName);
                        return NullRequest();
                    }
                }
            } else if(req.Type == "HEAD")
            {
                Dictionary<string, string> lst = new Dictionary<string, string>();
                lst.Add("Server", Server.SERVER);
                return new Response(Server.HTTP1V, Server.NO_CONTENT, lst, new char[0]);
            }
            else
            {
                Console.WriteLine("Method not allowed: " + req.Type);
                return new Response(Server.HTTP1V, "405 Method Not Allowed", null, new char[0]);
            }
            */
            #endregion
        }
        public void Send(char[] data)
        {
            this.Data = data;
        }

        private static HTTP1Response HTTP1Response(HTTP1Request req, bool headrequest=false)
        {
            Console.WriteLine("Responding with http/1.1...");
            Dictionary<string, string> lst = new Dictionary<string, string>();
            lst.Add("Server", Server.SERVER);
            if (headrequest) return new HTTP1Response(Server.HTTP1V, Server.NO_CONTENT, lst, new char[0]);
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
                        lst.Add("Content-Type", Mapping.MIME_MAP[fi.Extension]);
                        lst.Add("Accept-Ranges", "bytes");
                        lst.Add("Content-Length", d.Length.ToString());
                        lst.Add("Keep-Alive", "timeout=5, max=100");
                        lst.Add("Connection", "Keep-Alive");
                        //char[] outarray = new char[d.Length*2];
                        //Convert.ToBase64CharArray(d, 0, d.Length, outarray, 0);
                        return new HTTP1Response(Server.HTTP1V, Server.OK, lst, (headrequest)? new char[0] : d);
                    }
                }
            }
            else
            {
                Console.WriteLine("File not found" + fi.FullName);
                return NullRequest();
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
            return new HTTP1Response(Server.HTTP1V, Server.ERROR, null, new char[0]);
        }

        private HTTP1Response(string httpv, string status, IDictionary<string, string> headerlines, char[] data)
        {
            HTTPv = httpv;
            Status = status;
            HeaderLines = headerlines;
            Data = data;

        }

        private static HTTP1Response GetResponseForUpgradeToh2c()
        {
            Dictionary<string, string> lst = new Dictionary<string, string>
            {
                { "Connection", "Upgrade" },
                { "Upgrade", "h2c" }
            };
            char[] d = new char[0];
            return new HTTP1Response(Server.HTTP1V, Server.SWITCHING_PROTOCOLS, lst, d);
        }
        private static HTTP1Response GetResponseForUpgradeToh2()
        {
            Dictionary<string, string> lst = new Dictionary<string, string>
            {
                { "Connection", "Upgrade" },
                { "Upgrade", "h2" }
            };
            char[] d = new char[0];
            return new HTTP1Response(Server.HTTP1V, Server.SWITCHING_PROTOCOLS, lst, d);
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
    }
}
