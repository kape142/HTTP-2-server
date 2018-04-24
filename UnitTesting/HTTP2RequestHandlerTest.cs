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
        /*[Fact]
        public void TestGetDataFramesFromFile()
        {
            
            string url = Environment.CurrentDirectory + "\\TestFiles\\index.html";
            var client = new lib.HandleClient();
            StreamHandler streamHandler = new StreamHandler();
            long nrOfFrames = 0;
            using ( FileStream fs = new FileInfo(url).OpenRead())
            {
                nrOfFrames = fs.Length/HTTP2Frame.MaxFrameSize + ((fs.Length % HTTP2Frame.MaxFrameSize != 0) ? 1 : 0);
            }

            HTTPRequestHandler.SendFile(streamHandler, 0, url);
            Assert.True(streamHandler.GetIncommingStreams(0).Frames.Count == nrOfFrames);
        }
        */ 
    }
}
