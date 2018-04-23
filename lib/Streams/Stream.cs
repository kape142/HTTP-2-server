﻿using System;
using System.Collections.Generic;
using System.Text;
using lib.Frames;
using lib.HTTPObjects;
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

    internal class HTTP2Stream
    {
        public uint Id { get; private set; }
        public uint Weight{get; set;} = 16;
        public List<HTTP2Stream> dependencies;
        public uint Dependency { get; set; } = 0;
        public List<HTTP2Frame> Frames { get; set; }

        public StreamState State { get; set; } = StreamState.Idle;

        
        public HTTP2Stream(uint id, StreamState state=StreamState.Idle)
        {
            Id = id;
            Frames = new List<HTTP2Frame>();
        }

        //TODO
        void close()
        {
            State = StreamState.Closed;
        } 
        
        internal void EndOfHeaders()
        {
            // todo
            byte[] headerblockfragment = new byte[100];

        }
    }
}
