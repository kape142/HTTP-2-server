using System;
using System.Collections.Generic;
using System.Text;
using lib.Frames;
using lib.HTTPObjects;
using System.Diagnostics;

namespace lib.Streams
{
    internal class HTTP2Stream
    {
        public HTTP2Stream(uint id, StreamState state, uint WindowSize)
        {
            Id = id;
            Frames = new List<HTTP2Frame>();
            this.WindowSize = WindowSize;
            dependencies = new List<HTTP2Stream>();
            Console.WriteLine("ID: "+id+": "+new StackFrame(1).GetMethod().Name);
        }

        public uint Id { get; private set; }
        public uint Weight{get; set;} = 16;
        public List<HTTP2Stream> dependencies;
        public uint Dependency { get; set; } = 0;
        public List<HTTP2Frame> Frames { get; set; }
        public StreamState State { get; set; }
        public uint WindowSize { get; set; }
    }
}
