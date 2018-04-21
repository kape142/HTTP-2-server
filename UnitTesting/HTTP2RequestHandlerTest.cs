using lib.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using lib.HTTPObjects;
using System.IO;

namespace UnitTesting
{
    public class HTTP2RequestHandlerTest
    {
        [Fact]
        public void TestGetDataFramesFromFile()
        {
            string url = Environment.CurrentDirectory + "\\TestFiles\\index.html";
            StreamHandler streamHandler = new StreamHandler(null);
            long nrOfFrames = 0;
            using ( FileStream fs = new FileInfo(url).OpenRead())
            {
                nrOfFrames = fs.Length/HTTP2Frame.SETTINGS_MAX_FRAME_SIZE + ((fs.Length % HTTP2Frame.SETTINGS_MAX_FRAME_SIZE != 0) ? 1 : 0);
            }

            HTTPRequestHandler.SendFile(streamHandler, 0, url);
            Assert.True(streamHandler.GetIncomming(0).Frames.Count == nrOfFrames);
        }
    }
}
