using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using lib.Streams;

namespace lib.HTTPObjects
{
    public class HTTPRequestHandler
    {
        private void GetDataFramesFromFile(Streams.HTTP2Stream stream, string url)
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
            byte[] d = new byte[HTTP2Frame.SETTINGS_MAX_FRAME_SIZE];
            for(long i = 0;  i<length; i+= HTTP2Frame.SETTINGS_MAX_FRAME_SIZE)
            {
                reader.Read(d, HTTP2Frame.SETTINGS_MAX_FRAME_SIZE, (int) i);
                //stream.AddFrame(new HTTP2Frame((int) stream.Id).AddDataPayload(d));
            }
            int rest = (int) length % HTTP2Frame.SETTINGS_MAX_FRAME_SIZE;
            reader.Read(d, (int)length-rest, rest);
        }
    }
}
