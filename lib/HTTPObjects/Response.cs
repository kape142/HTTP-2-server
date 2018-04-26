using lib.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib.HTTPObjects
{
    public class Response : IResponse
    {
        private StreamHandler _streamHandler;
        private int _streamId;
        internal Response(StreamHandler streamHandler, int streamId)
        {
            _streamHandler = streamHandler;
            _streamId = streamId;
        }

        public void Send(string data)
        {
            Send(Encoding.ASCII.GetBytes(data), "text/plain");
        }

        private void Send(byte[] data, string contentType)
        {
            HTTP2RequestGenerator.SendData(_streamHandler, _streamId, data, contentType);
        }

        
    }
}
