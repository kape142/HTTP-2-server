using System;
using System.Collections.Generic;
using System.Text;

namespace lib
{
    public class HTTP1Request
    {
        public HTTP1Request(string data)
        {
            if (data.Length < 5) throw new ArgumentException("The request does not contains enough data");
            HeaderLines = new Dictionary<string, string>();
            string[] lines = data.ToString().Split('\n');
            string[] requestline = lines[0].Split(' ');
            Type = requestline[0];
            HttpUrl = requestline[1].Replace("/", "");
            Httpv = requestline[2];

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length < 5) continue;
                int split = lines[i].IndexOf(':');
                if (lines[i].Substring(0, split).Trim() == "Upgrade") IsUpgradeTo2 = true;
                HeaderLines.Add(lines[i].Substring(0, split).Trim(), lines[i].Substring(split + 1).Trim());
            }
        }

        public string Type { get; private set; }
        public string HttpUrl { get; private set; }
        public string Httpv { get; set; }
        public IDictionary<string, string> HeaderLines { get; set; }
        public bool IsUpgradeTo2 { get; set; }

        public override string ToString()
        {
            string ret = "";
            foreach (var item in HeaderLines)
            {
                ret += item.Key + " : " + item.Value + "\n";
            }
            return Type + " " + HttpUrl + " " + Httpv + "\n" + ret;
        }
    }
}
