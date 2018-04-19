using lib.HTTPObjects;
using System;
using System.Collections.Generic;

namespace HTTP2Server
{
    public class Server
    {
        Dictionary<string, Func<HTTPRequest, HTTPResponse>> restLibrary = new Dictionary<string, Func<HTTPRequest, HTTPResponse>>();

        public void Get(string path, Func<HTTPRequest, HTTPResponse> callback)
        {
            restLibrary.Add("GET/"+path, callback);
        }

        public void Post(string path, Func<HTTPRequest, HTTPResponse> callback)
        {
            restLibrary.Add("POST/" + path, callback);
        }

        public static void test()
        {
            var fc = new Frame(8577);
            Console.WriteLine(fc.ToString());

            fc.addSettingsPayload(0x0,new Tuple<short, int>[] {new Tuple<short,int>(Frame.SETTINGS_MAX_FRAME_SIZE,128) });
            var by = fc.getBytes();
            foreach (byte b in by)
                Console.Write($"{b} ");
            Console.WriteLine();
            Console.WriteLine(fc.ToString());

        }
    }
}
