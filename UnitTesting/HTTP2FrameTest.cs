using System;
using Xunit;
using lib;
using lib.HTTPObjects;

namespace UnitTest
{
    public class HTTP2FrameTest
    {

        [Fact]
        public void TestAddSettingsPayload()
        {
            var frame = new HTTP2Frame(8);
            var settings = new Tuple<short, int>[2];
            settings[0] = new Tuple<short, int>(HTTP2Frame.SETTINGS_INITIAL_WINDOW_SIZE, 0x1000);
            settings[1] = new Tuple<short, int>(HTTP2Frame.SETTINGS_ENABLE_PUSH, 0x0);
            frame.addSettingsPayload(settings, true);
            byte[] bytes = frame.getBytes();
            Assert.Equal(12, HTTP2Frame.ConvertFromIncompleteByteArray(new byte[] {bytes[0],bytes[1],bytes[2]}));
            Assert.Equal(HTTP2Frame.SETTINGS, bytes[3]);
            Assert.Equal(HTTP2Frame.ACK, bytes[4]);
            Assert.Equal(8, bytes[8]);
            Assert.Equal(HTTP2Frame.SETTINGS_INITIAL_WINDOW_SIZE, HTTP2Frame.ConvertFromIncompleteByteArray(new byte[] { bytes[9], bytes[10]}));
            Assert.Equal(0x1000, HTTP2Frame.ConvertFromIncompleteByteArray(new byte[] { bytes[11], bytes[12], bytes[13], bytes[14] }));
            Assert.Equal(HTTP2Frame.SETTINGS_ENABLE_PUSH, HTTP2Frame.ConvertFromIncompleteByteArray(new byte[] { bytes[15], bytes[16] }));
            Assert.Equal(0x0, HTTP2Frame.ConvertFromIncompleteByteArray(new byte[] { bytes[17], bytes[18], bytes[19], bytes[20] }));
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
