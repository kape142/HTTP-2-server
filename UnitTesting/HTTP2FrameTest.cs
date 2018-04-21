using System;
using Xunit;
using lib;
using lib.HTTPObjects;
using static lib.HTTPObjects.HTTP2Frame;

namespace UnitTest
{
    public class HTTP2FrameTest
    {

        [Fact]
        public void TestAddSettingsPayload()
        {
            var frame = new HTTP2Frame(8);
            var settings = new Tuple<short, int>[2];
            settings[0] = new Tuple<short, int>(SETTINGS_INITIAL_WINDOW_SIZE, 0x1000);
            settings[1] = new Tuple<short, int>(SETTINGS_ENABLE_PUSH, 0x0);
            frame.addSettingsPayload(settings, true);
            byte[] bytes = frame.getBytes();
            Assert.Equal(12, ConvertFromIncompleteByteArray(GetPartOfByteArray(0,3,bytes)));
            Assert.Equal(SETTINGS, bytes[3]);
            Assert.Equal(ACK, bytes[4]);
            Assert.Equal(8, bytes[8]);
            Assert.Equal(SETTINGS_INITIAL_WINDOW_SIZE, ConvertFromIncompleteByteArray(GetPartOfByteArray(9, 11, bytes)));
            Assert.Equal(0x1000, ConvertFromIncompleteByteArray(GetPartOfByteArray(11, 15, bytes)));
            Assert.Equal(SETTINGS_ENABLE_PUSH, ConvertFromIncompleteByteArray(GetPartOfByteArray(15, 17, bytes)));
            Assert.Equal(0x0, ConvertFromIncompleteByteArray(GetPartOfByteArray(17, 21, bytes)));
        }

        [Fact]
        public void TestAddDataPayload()
        {
            var frame = new HTTP2Frame(127);
            int data = 174517637;
            frame.AddDataPayload(ExtractBytes(data), paddingLength:16);
            byte[] bytes = frame.getBytes();
            Assert.Equal(21, ConvertFromIncompleteByteArray(GetPartOfByteArray(0, 3, bytes)));
            Assert.Equal(DATA, bytes[3]);
            Assert.Equal(PADDED, bytes[4]);
            Assert.Equal(127, ConvertFromIncompleteByteArray(GetPartOfByteArray(5, 9, bytes)));
            Assert.Equal(16, bytes[9]);
            Assert.Equal(data, ConvertFromIncompleteByteArray(GetPartOfByteArray(10, 14, bytes)));
            Assert.Equal(new byte[16], GetPartOfByteArray(14, 30, bytes));

            frame = new HTTP2Frame(5234);
            data = 523978457;
            frame.AddDataPayload(ExtractBytes(data), endStream: true);
            bytes = frame.getBytes();
            Assert.Equal(4, ConvertFromIncompleteByteArray(GetPartOfByteArray(0, 3, bytes)));
            Assert.Equal(DATA, bytes[3]);
            Assert.Equal(END_STREAM, bytes[4]);
            Assert.Equal(5234, ConvertFromIncompleteByteArray(GetPartOfByteArray(5,9,bytes)));
            Assert.Equal(data, ConvertFromIncompleteByteArray(GetPartOfByteArray(9, 13, bytes)));
        }

        [Fact]
        public void TestSplit32BitToBoolAnd31bitInt()
        {
            uint _uint = 0b10000000000000000000000000000000;
            int test = (int)(_uint | 0b01111000000000000000000000000000); // 1 and 2013265920
            int test1 = (int)(_uint | 0b00000000000000000000000000001111); // 1 and 15
            int test2 = 0b01111000000000000000000000000000; // 0 and 2013265920
            int test3 = 0b00000000000000000000000000001111; // 0 and 15

            var t = HTTP2Frame.Split32BitToBoolAnd31bitInt(test);
            Assert.True(t.bit32);
            Assert.True(2013265920 == t.int31);

            t = HTTP2Frame.Split32BitToBoolAnd31bitInt(test1);
            Assert.True(t.bit32);
            Assert.True(15 == t.int31);

            t = HTTP2Frame.Split32BitToBoolAnd31bitInt(test2);
            Assert.False(t.bit32);
            Assert.True(2013265920 == t.int31);

            t = HTTP2Frame.Split32BitToBoolAnd31bitInt(test3);
            Assert.False(t.bit32);
            Assert.True(15 == t.int31);
        }

        [Fact]
        public void TestExtractBytes()
        {
            long l = 1431823481723234123L;
            byte[] b = HTTP2Frame.ExtractBytes(l);
            foreach (byte by in b)
                Console.Write($"{by} ");
            Array.Reverse(b);
            Assert.Equal(BitConverter.ToInt64(b, 0),l);

            int i = 1823423647;
            b = HTTP2Frame.ExtractBytes(i);
            Array.Reverse(b);
            Assert.Equal(BitConverter.ToInt32(b, 0),i);

            short s = 12364;
            b = HTTP2Frame.ExtractBytes(s);
            Array.Reverse(b);
            Assert.Equal(BitConverter.ToInt16(b, 0),s);
        }

        [Fact]
        public void TestConvertFromIncompleteByteArray()
        {
            int i = 1823423647;
            var b = BitConverter.GetBytes(i);
            Array.Reverse(b);
            Assert.Equal(HTTP2Frame.ConvertFromIncompleteByteArray(b), i);
        }


    }
}
