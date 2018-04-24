using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using lib.HTTPObjects;
using lib.Streams;

namespace lib.HTTPObjects
{
    public class HTTPResponse
    {
        private void GetDataFramesFromFile(Stream stream, string url)
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
            if (!fi.Exists) return;
            FileStream fs = fi.OpenRead();
            BinaryReader reader = new BinaryReader(fs);
            long length = fs.Length;
            byte[] d = new byte[HTTP2Frame.MaxFrameSize];
            for (long i = 0; i < length; i += HTTP2Frame.MaxFrameSize)
            {
                reader.Read(d, HTTP2Frame.MaxFrameSize, (int)i);
                //stream.addFrame(new HTTP2Frame((int)stream.Id).AddDataPayload(d));
            }
            int rest = (int)length % HTTP2Frame.MaxFrameSize;
            reader.Read(d, (int)length - rest, rest);
        }
    }
}
