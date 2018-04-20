using System;
using System.Collections.Generic;
using System.Text;

namespace lib.HTTPObjects
{
    class HTTP2Frame
    {
        private static readonly bool littleEndian = BitConverter.IsLittleEndian;
        public const int headerSize = 9;

        //Frame Types
        public const byte DATA = 0x0;
        public const byte HEADERS = 0x1;
        public const byte PRIORITY_TYPE = 0x2;
        public const byte RST_STREAM = 0x3;
        public const byte SETTINGS = 0x4;
        public const byte PUSH_PROMISE = 0x5;
        public const byte PING = 0x6;
        public const byte GOAWAY = 0x7;
        public const byte WINDOW_UPDATE = 0x8;
        public const byte CONTINUATION = 0x9;

        //Flags
        public const byte NO_FLAG = 0x0;
        public const byte END_STREAM = 0x1;
        public const byte END_HEADERS = 0x4;
        public const byte PADDED = 0x8;
        public const byte PRIORITY_FLAG = 0x20;
        public const byte ACK = 0x1;

        //Settings Parameters
        public const byte SETTINGS_HEADER_TABLE_SIZE = 0x1;
        public const byte SETTINGS_ENABLE_PUSH = 0x2;
        public const byte SETTINGS_MAX_CONCURRENT_STREAMS = 0x3;
        public const byte SETTINGS_INITIAL_WINDOW_SIZE = 0x4;
        public const byte SETTINGS_MAX_FRAME_SIZE = 0x5;
        public const byte SETTINGS_MAX_HEADER_LIST_SIZE = 0x6;

        //Error codes
        public const byte NO_ERROR = 0x0;
        public const byte PROTOCOL_ERROR = 0x1;
        public const byte INTERNAL_ERROR = 0x2;
        public const byte FLOW_CONTROL_ERROR = 0x3;
        public const byte SETTINGS_TIMEOUT = 0x4;
        public const byte STREAM_CLOSED = 0x5;
        public const byte FRAME_SIZE_ERROR = 0x6;
        public const byte REFUSED_STREAM = 0x7;
        public const byte CANCEL = 0x8;
        public const byte COMPRESSION_ERROR = 0x9;
        public const byte CONNECT_ERROR = 0xa;
        public const byte ENHANCE_YOUR_CALM = 0xb;
        public const byte INADEQUATE_SECURITY = 0xc;
        public const byte HTTP_1_1_REQUIRED = 0xd;

        //Object Variables
        private static int maxFrameSize = 16384;
        private byte[] byteArray;

        //Properties
        public int FrameLength
        {
            get
            {
                return byteArray.Length;
            }
        }
        public byte Type
        {
            get{
                return byteArray[3];
            }
            private set
            {
                byteArray[3] = value;
            }
        }

        public byte Flag
        {
            get
            {
                return byteArray[4];
            }
            private set
            {
                byteArray[4] = value;
            }
        }

        public int PayloadLength
        {
            get
            {
                return ConvertFromIncompleteByteArray(GetPartOfByteArray(0, 3));
            }
        }

        public int StreamIdentifier
        {
            get
            {
                return ConvertFromIncompleteByteArray(GetPartOfByteArray(5, 9));
            }
        }

        public byte[] Header
        {
            get
            {
                return GetPartOfByteArray(0, 9);
            }
        }
        
        public byte[] Payload
        {
            get
            {
                return GetPartOfByteArray(9, this.PayloadLength);
            }
            private set
            {
                if (value.Length > maxFrameSize)
                    throw new Exception($"Cannot create frame larger than maxFrameSize, {maxFrameSize}"); //TODO error handling?
                byte[] b = ConvertTo24BitNumber(value.Length);
                for (int i = 0; i < 3; i++)
                    byteArray[i] = b[i];

                byte[] header = this.Header;
                byte[] frame = new byte[headerSize + value.Length];
                for (int i = 0; i < headerSize + value.Length; i++)
                    frame[i] = (i < headerSize) ? header[i] : value[i-headerSize];
                byteArray = frame;
            }
        }
        


        public HTTP2Frame(byte[] byteArray)
        {
            this.byteArray = byteArray;
        }

        public HTTP2Frame(int streamIdentifier)
        {
            var array = new byte[headerSize];
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
            s.Append("Type: ");
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
            s.Append($"Flag: {this.Flag}, ");
            s.Append($"Length: {this.PayloadLength}, ");
            s.Append($"Stream identifier: {this.StreamIdentifier}");

            return s.ToString();

        }

        public HTTP2Frame addSettingsPayload(Tuple<short, int>[] settings, bool ack = false)
        {
            Type = SETTINGS;
            Flag = ack?ACK:NO_FLAG;

            var array = new byte[settings.Length*6];
            int i = 0;
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
            Payload = array;
            return this;
        }


        public HTTP2Frame AddDataPayload(byte[] data, byte paddingLength = 0x0)
        {
            Type = DATA;
            
            if (paddingLength == 0x0)
            {
                Payload = data;
                Flag = NO_FLAG;
            }
            else
            {
                Flag = PADDED;
                var array = new byte[1 + data.Length + paddingLength];
                array[0] = paddingLength;
                for (int i = 0; i < data.Length; i++)
                    array[i + 1] = data[i];
                Payload = array;
            }
            return this;
        }

        public HTTP2Frame AddHeaderPayload(byte[] data, bool end_stream = false, bool end_headers = false, byte paddingLength = 0x0, bool priority = false)
        {
            byte flag = (byte)((end_stream ? END_STREAM : NO_FLAG) | (end_headers ? END_HEADERS : NO_FLAG) | ((paddingLength==0x0)?PADDED:NO_FLAG) | (priority? PRIORITY_FLAG:NO_FLAG));
            Type = DATA;
            Flag = flag;
            Payload = data;
            return this;
        }


        public static int ConvertFromIncompleteByteArray(byte[] array)
        {
            bool littleEndian = BitConverter.IsLittleEndian;
            byte[] target = new byte[4];
            for (int i = 0; i < array.Length; i++)
            {
                int j = littleEndian ? 0 : array.Length;
                target[j] = array[i];
                j += littleEndian ? 1 : -1;
            }
            return BitConverter.ToInt32(target, 0);
        }

        public static byte[] ConvertTo24BitNumber(int number)
        {
            var byteArr = new byte[3];
            var numArr = BitConverter.GetBytes(number);
            if (littleEndian) Array.Reverse(numArr);
            for (int j = 0; j < 3; j++)
                byteArr[j] = numArr[j + 1];
            return byteArr;
        }

        private byte[] GetPartOfByteArray(int start, int end)
        {
            byte[] part = new byte[end - start];
            for(int i = 0; i< end - start; i++)
            {
                part[i] = byteArray[start + i];
            }
            return part;
        }
    }
}

