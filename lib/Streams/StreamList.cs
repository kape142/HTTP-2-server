using System;
using System.Collections.Generic;
using System.Text;

namespace lib.Streams
{
    class StreamList
    {
        private Dictionary<uint, HTTP2Stream> streams = new Dictionary<uint, HTTP2Stream>();
        internal HandleClient owner;
        internal StreamList(HandleClient owner)
        {
            this.owner = owner;
        }

        public HTTP2Stream Get(uint index)
        {
            if (streams.ContainsKey(index))
            {
                return streams.GetValueOrDefault(index);
            }
            else
            {
                HTTP2Stream stream = new HTTP2Stream(index, StreamState.Idle, owner.settings.InitialWindowSize);
                return stream;
            }
        }

        public void Add(HTTP2Stream stream)
        {
            if (streams.ContainsKey(stream.Id))
            {
                var oldStream = Get(stream.Id);
                oldStream.State = stream.State;
                oldStream.WindowSize = stream.WindowSize;
            }
        }
    }
}
