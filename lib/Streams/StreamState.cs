using System;
using System.Collections.Generic;
using System.Text;

namespace lib.Streams
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
}
