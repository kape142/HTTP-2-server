using System;
using System.Collections.Generic;
using System.Text;

namespace lib.Frames
{
    public struct HeaderPayload
    {
        public byte PadLength;
        public bool StreamDependencyIsExclusive;
        public int StreamDependency;
        public byte Weight;
        public HeaderBlockFragment HeaderBlockFragment;
    }
}
