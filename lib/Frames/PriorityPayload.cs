using System;
using System.Collections.Generic;
using System.Text;


namespace lib.Frames
{
    struct PriorityPayload
    {
        public bool StreamDependencyIsExclusive;
        public int StreamDependency;
        public byte Weight;
    }
}
