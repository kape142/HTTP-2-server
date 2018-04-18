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
            var fc = new Frame();
            Console.WriteLine();
            var settingsframe = fc.createSettingsFrame(0x0,0x0,new Tuple<short, int>[] {new Tuple<short,int>(Frame.SETTINGS_MAX_FRAME_SIZE,128) });
            foreach (byte b in settingsframe)
                Console.Write($"{b} ");

        }
    }
}
