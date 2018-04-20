using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lib
{
    class HTTP2ResponsHandler
    {
        private byte[] GetFileData(string url)
        {
            String file = null;
            if (url == "")
            {
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\index.html";
            }
            else
            {
                file = Environment.CurrentDirectory + "\\" + Server.DIR + "\\" + url;
            }
            FileInfo fi = new FileInfo(file);
            if (!fi.Exists) return null;
            FileStream fs = fi.OpenRead();
            BinaryReader reader = new BinaryReader(fs);
            byte[] d = new byte[fs.Length];
            reader.Read(d, 0, d.Length);
            return d;
        }

    }
}
