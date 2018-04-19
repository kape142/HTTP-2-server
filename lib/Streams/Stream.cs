using System;
using System.Collections.Generic;
using System.Text;

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
        public uint Priority
        {
            get; set;
        }
        public StreamState State
        {
            get;set;
        }

        //TODO
        void close()
        {
            State = StreamState.Closed;
        }    


    }
}
