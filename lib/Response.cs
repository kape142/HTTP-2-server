using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lib
{
    class Response
    {
        public string HTTPv { get; private set; }
        public string Status { get; private set; }
        public IDictionary<string, string> HeaderLines { get; private set; }
        public char[] Data { get; private set; }

        public static Response From(Request req)
        {
            if (req == null)
            {
                return NullRequest();
            }
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
            }
            else
            {
                return new Response(Server.HTTP1V, "405 Method Not Allowed", null, new char[0]);
            }
        }

        private static Response NullRequest()
        {
            return new Response(Server.HTTP1V, Server.ERROR, null, new char[0]);
        }

        public Response(string httpv, string status, IDictionary<string, string> headerlines, char[] data)
        {
            HTTPv = httpv;
            Status = status;
            HeaderLines = headerlines;
            Data = data;

        }

        private static Response UpgradeToh2c()
        {
            Dictionary<string, string> lst = new Dictionary<string, string>();
            lst.Add("Connection", "Upgrade");
            lst.Add("Upgrade", "h2c");
            char[] d = null;
            return new Response(Server.HTTP1V, Server.SWITCHING_PROTOCOLS, lst, d);
        }
        private static Response UpgradeToh2()
        {
            Dictionary<string, string> lst = new Dictionary<string, string>();
            lst.Add("Connection", "Upgrade");
            lst.Add("Upgrade", "h2");
            char[] d = null;
            return new Response(Server.HTTP1V, Server.SWITCHING_PROTOCOLS, lst, d);
        }

        public override string ToString()
        {
            string ret = HTTPv + " " + Status + "\n";
            if (HeaderLines != null)
            {
                foreach (var item in HeaderLines)
                {
                    ret += item.Key + " : " + item.Value + "\n";
                }
            }
            return ret;
        }
    }
}
