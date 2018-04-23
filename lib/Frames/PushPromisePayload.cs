using System;
using System.Collections.Generic;
using System.Text;

namespace lib.Frames
{
    public struct PushPromisePayload
    {
        public byte PadLength;
        public int PromisedStreamID;
        public byte[] HeaderBlockFragment;
    }
}
