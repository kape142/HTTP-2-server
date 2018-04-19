using System;
using System.Collections.Generic;
using System.Text;

namespace lib.HTTPObjects
{
    class Frame
    {
        private static readonly bool littleEndian = BitConverter.IsLittleEndian;
        private static readonly int headerSize = 9;

        //Frame Types
        public static readonly byte DATA = 0x0;
        public static readonly byte HEADERS = 0x1;
        public static readonly byte PRIORITY_TYPE = 0x2;
        public static readonly byte RST_STREAM = 0x3;
        public static readonly byte SETTINGS = 0x4;
        public static readonly byte PUSH_PROMISE = 0x5;
        public static readonly byte PING = 0x6;
        public static readonly byte GOAWAY = 0x7;
        public static readonly byte WINDOW_UPDATE = 0x8;
        public static readonly byte CONTINUATION = 0x9;

        //Flags
        public static readonly byte END_STREAM = 0x1;
        public static readonly byte END_HEADERS = 0x4;
        public static readonly byte PADDED = 0x8;
        public static readonly byte PRIORITY_FLAG = 0x20;
        public static readonly byte ACK = 0x1;

        //Settings Parameters
        public static readonly byte SETTINGS_HEADER_TABLE_SIZE = 0x1;
        public static readonly byte SETTINGS_ENABLE_PUSH = 0x2;
        public static readonly byte SETTINGS_MAX_CONCURRENT_STREAMS = 0x3;
        public static readonly byte SETTINGS_INITIAL_WINDOW_SIZE = 0x4;
        public static readonly byte SETTINGS_MAX_FRAME_SIZE = 0x5;
        public static readonly byte SETTINGS_MAX_HEADER_LIST_SIZE = 0x6;

        //Error codes
        public static readonly byte NO_ERROR = 0x0;
        public static readonly byte PROTOCOL_ERROR = 0x1;
        public static readonly byte INTERNAL_ERROR = 0x2;
        public static readonly byte FLOW_CONTROL_ERROR = 0x3;
        public static readonly byte SETTINGS_TIMEOUT = 0x4;
        public static readonly byte STREAM_CLOSED = 0x5;
        public static readonly byte FRAME_SIZE_ERROR = 0x6;
        public static readonly byte REFUSED_STREAM = 0x7;
        public static readonly byte CANCEL = 0x8;
        public static readonly byte COMPRESSION_ERROR = 0x9;
        public static readonly byte CONNECT_ERROR = 0xa;
        public static readonly byte ENHANCE_YOUR_CALM = 0xb;
        public static readonly byte INADEQUATE_SECURITY = 0xc;
        public static readonly byte HTTP_1_1_REQUIRED = 0xd;

        //Object Variables
        private static int maxFrameSize = 16384;
        private byte[] byteArray;
       

        public Frame(byte[] byteArray)
        {
            this.byteArray = byteArray;
        }

        public Frame(int streamIdentifier)
        {
            streamIdentifier = (int)Math.Abs(streamIdentifier);
            var array = new byte[headerSize];
            var lengthArr = BitConverter.GetBytes(0);
            if (littleEndian) Array.Reverse(lengthArr);
            for (int i = 0; i < 3; i++)
                array[i] = lengthArr[i + 1];

            array[3] = 0x0;
            array[4] = 0x0;
            var streamIdentifierArr = BitConverter.GetBytes(streamIdentifier);
            if (littleEndian) Array.Reverse(streamIdentifierArr);
            for (int i = 0; i < 4; i++)
                array[5 + i] = streamIdentifierArr[i];

            this.byteArray = array;
        }

        public byte[] getBytes()
        {
            var b = new byte[byteArray.Length];
            Array.Copy(byteArray, b, byteArray.Length);
            return b;
        }

        
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            StringBuilder payload = new StringBuilder();
            switch (byteArray[3])
            {
                case 0x0:
                    s.Append("Data");
                    break;
                case 0x1:
                    s.Append("Headers");
                    break;
                case 0x2:
                    s.Append("Priority");
                    break;
                case 0x3:
                    s.Append("RST Stream");
                    break;
                case 0x4:
                    s.Append("Settings");
                    
                    break;
                case 0x5:
                    s.Append("Push Promise");
                    break;
                case 0x6:
                    s.Append("Ping");
                    break;
                case 0x7:
                    s.Append("GoAway");
                    break;
                case 0x8:
                    s.Append("Window Update");
                    break;
                case 0x9:
                    s.Append("Continuation");
                    break;
                default:
                    s.Append("Undefined");
                    break;
            }
            s.Append(", ");
            s.Append($"Flag: {byteArray[4]}, ");
            byte[] lengthArr = new byte[4];
            for(int i = 0; i < 3;i++)
            {
                int j = littleEndian ? 0 : 3;
                lengthArr[j] = byteArray[i];
                j += littleEndian ? 1 : -1;
            }
            int length = BitConverter.ToInt32(lengthArr,0);
            s.Append($"Length: {length}, ");

            byte[] sidArr = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                int j = littleEndian ? 0 : 3;
                lengthArr[j] = byteArray[5+i];
                j += littleEndian ? 1 : -1;
            }
            int sid = BitConverter.ToInt32(lengthArr, 0);
            s.Append($"Stream identifier: {sid}");

            return s.ToString();
        }

        public Frame addSettingsPayload(byte flag, Tuple<short, int>[] settings)
        {
            int length = settings.Length * 6;
            if (length > maxFrameSize)
                throw new Exception($"Cannot create frame larger than maxFrameSize, {maxFrameSize}"); //TODO error handling?
            
            var lengthArr = BitConverter.GetBytes(length);
            if (littleEndian) Array.Reverse(lengthArr);
            for (int j = 0; j < 3; j++)
                byteArray[j] = lengthArr[j + 1];

            byteArray[3] = SETTINGS;
            byteArray[4] = flag;

            var array = new byte[length + headerSize];
            int i= 0;
            for (; i < headerSize; i++)
                array[i] = byteArray[i];

            foreach (var tuple in settings)
            {
                var item1bytes = BitConverter.GetBytes(tuple.Item1);
                if (littleEndian) Array.Reverse(item1bytes);
                foreach (byte b in item1bytes)
                    array[i++] = b;

                var item2bytes = BitConverter.GetBytes(tuple.Item2);
                if (littleEndian) Array.Reverse(item2bytes);
                foreach (byte b in item2bytes)
                    array[i++] = b;
            }
            byteArray = array;
            return this;
        }
       
    }
}

