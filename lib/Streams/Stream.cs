using System;
using System.Collections.Generic;
using System.Text;
using lib.HTTPObjects;
namespace lib
{
    public enum StreamState
    {
        Idle,
        ReservedLocal,
        ReservedRemote,
        Open,
        HalfClosedLocal,
        HalfClosedRemote,
        Closed

    }
    class Stream
    {
        public uint Id
        {
            get;
        }
        public uint Weight{get; set;} = 16;
        public List<Stream> dependencies;
        public uint Dependency { get; set; } = 0;
        public Queue<Frame> frames;


        public StreamState State { get; set; } = StreamState.Idle;

        public Stream() { }
        
        public Stream(uint id){Id = id;}

        //TODO
        void close()
        {
            State = StreamState.Closed;
        }    
    }
}
