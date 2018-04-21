using lib.Frames;
using lib.HTTPObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace lib
{
    public class HTTP2ResponsHandler
    {
        internal static void Handle(HTTP2Frame frame)
        {
            switch (frame.Type)
            {
                case HTTP2Frame.DATA:
                    break;
                case HTTP2Frame.HEADERS:
                    HeaderPayload hp = frame.GetHeaderPayloadDekoded();

                    break;
                case HTTP2Frame.PRIORITY_TYPE:
                    break;
                case HTTP2Frame.RST_STREAM:
                    break;
                case HTTP2Frame.SETTINGS:
                    break;
                case HTTP2Frame.PUSH_PROMISE:
                    break;
                case HTTP2Frame.PING:
                    break;
                case HTTP2Frame.GOAWAY:
                    break;
                case HTTP2Frame.WINDOW_UPDATE:
                    break;
                case HTTP2Frame.CONTINUATION:
                    break;
                default:
                    break;
            }
        }

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
