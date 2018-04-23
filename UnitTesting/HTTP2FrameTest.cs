using System;
using Xunit;
using lib;
using lib.HTTPObjects;
using static lib.HTTPObjects.HTTP2Frame;
using static lib.Bytes;
using lib.Frames;

namespace UnitTest
{
    public class HTTP2FrameTest
    {

        [Fact]
        public void TestAddSettingsPayload()
        {
            var frame = new HTTP2Frame(8);
            var settings = new(ushort, uint)[] { (SETTINGS_INITIAL_WINDOW_SIZE, 0x1000), (SETTINGS_ENABLE_PUSH, 0x0) };
            frame.AddSettingsPayload(settings, true);
            byte[] bytes = frame.GetBytes();
            Assert.Equal(12, ConvertFromIncompleteByteArray(GetPartOfByteArray(0,3,bytes)));
            Assert.Equal(SETTINGS, bytes[3]);
            Assert.Equal(ACK, bytes[4]);
            Assert.Equal(8, bytes[8]);
            Assert.Equal(SETTINGS_INITIAL_WINDOW_SIZE, ConvertFromIncompleteByteArray(GetPartOfByteArray(9, 11, bytes)));
            Assert.Equal(0x1000, ConvertFromIncompleteByteArray(GetPartOfByteArray(11, 15, bytes)));
            Assert.Equal(SETTINGS_ENABLE_PUSH, ConvertFromIncompleteByteArray(GetPartOfByteArray(15, 17, bytes)));
            Assert.Equal(0x0, ConvertFromIncompleteByteArray(GetPartOfByteArray(17, 21, bytes)));

            SettingsPayload sp = frame.GetSettingsPayloadDecoded();
            Assert.Equal(settings, sp.Settings);
        }

        [Fact]
        public void TestAddRSTPayload()
        {
            var frame = new HTTP2Frame(234);
            uint error = 12938;
            frame.AddRSTStreamPayload(error);
            byte[] bytes = frame.GetBytes();
            Assert.Equal(4, ConvertFromIncompleteByteArray(GetPartOfByteArray(0, 3, bytes)));
            Assert.Equal(RST_STREAM, bytes[3]);
            Assert.Equal(NO_FLAG, bytes[4]);
            Assert.Equal(234, bytes[8]);
            Assert.Equal(error, ConvertFromIncompleteByteArrayUnsigned(GetPartOfByteArray(9, 13, bytes)));

            RSTStreamPayload rp = frame.GetRSTStreamPayloadDecoded();
            Assert.Equal(error, rp.ErrorCode);
        }

        [Fact]
        public void TestAddPushPromisePayload()
        {
            var frame = new HTTP2Frame(46);
            int psi = 1783;
            byte[] hbf = { 21, 34, 3, 4, 23, 4, 35, 3 };
            frame.AddPushPromisePayload(psi, hbf, endHeaders: true);
            byte[] bytes = frame.GetBytes();
            Assert.Equal(12, ConvertFromIncompleteByteArray(GetPartOfByteArray(0, 3, bytes)));
            Assert.Equal(PUSH_PROMISE, bytes[3]);
            Assert.Equal(END_HEADERS, bytes[4]);
            Assert.Equal(46, bytes[8]);
            Assert.Equal(psi, ConvertFromIncompleteByteArray(GetPartOfByteArray(9, 13, bytes)));
            Assert.Equal(hbf, GetPartOfByteArray(13, 21, bytes));

            PushPromisePayload pp = frame.GetPushPromisePayloadDecoded();
            Assert.Equal(psi, pp.PromisedStreamID);
            Assert.Equal(hbf, pp.HeaderBlockFragment);
            Assert.Equal(0, pp.PadLength);
        }

        [Fact]
        public void TestAddDataPayload()
        {
            var frame = new HTTP2Frame(127);
            int data = 174517637;
            frame.AddDataPayload(ExtractBytes(data), paddingLength:16);
            byte[] bytes = frame.GetBytes();
            Assert.Equal(21, ConvertFromIncompleteByteArray(GetPartOfByteArray(0, 3, bytes)));
            Assert.Equal(DATA, bytes[3]);
            Assert.Equal(PADDED, bytes[4]);
            Assert.Equal(127, ConvertFromIncompleteByteArray(GetPartOfByteArray(5, 9, bytes)));
            Assert.Equal(16, bytes[9]);
            Assert.Equal(data, ConvertFromIncompleteByteArray(GetPartOfByteArray(10, 14, bytes)));
            Assert.Equal(new byte[16], GetPartOfByteArray(14, 30, bytes));

            DataPayload dp = frame.GetDataPayloadDecoded();
            Assert.Equal(ExtractBytes(data), dp.Data);
            Assert.Equal(16, dp.PadLength);


            frame = new HTTP2Frame(5234);
            data = 523978457;
            frame.AddDataPayload(ExtractBytes(data), endStream: true);
            bytes = frame.GetBytes();
            Assert.Equal(4, ConvertFromIncompleteByteArray(GetPartOfByteArray(0, 3, bytes)));
            Assert.Equal(DATA, bytes[3]);
            Assert.Equal(END_STREAM, bytes[4]);
            Assert.Equal(5234, ConvertFromIncompleteByteArray(GetPartOfByteArray(5,9,bytes)));
            Assert.Equal(data, ConvertFromIncompleteByteArray(GetPartOfByteArray(9, 13, bytes)));

            dp = frame.GetDataPayloadDecoded();
            Assert.Equal(ExtractBytes(data), dp.Data);
            Assert.Equal(0, dp.PadLength);
        }

        [Fact]
        public void TestSplit32BitToBoolAnd31bitInt()
        {
            uint _uint = 0b10000000000000000000000000000000;
            int test = (int)(_uint | 0b01111000000000000000000000000000); // 1 and 2013265920
            int test1 = (int)(_uint | 0b00000000000000000000000000001111); // 1 and 15
            int test2 = 0b01111000000000000000000000000000; // 0 and 2013265920
            int test3 = 0b00000000000000000000000000001111; // 0 and 15

            var t = Split32BitToBoolAnd31bitInt(test);
            Assert.True(t.bit32);
            Assert.True(2013265920 == t.int31);

            t = Split32BitToBoolAnd31bitInt(test1);
            Assert.True(t.bit32);
            Assert.True(15 == t.int31);

            t = Split32BitToBoolAnd31bitInt(test2);
            Assert.False(t.bit32);
            Assert.True(2013265920 == t.int31);

            t = Split32BitToBoolAnd31bitInt(test3);
            Assert.False(t.bit32);
            Assert.True(15 == t.int31);
        }

        [Fact]
        public void TestExtractBytes()
        {
            long l = 1431823481723234123L;
            byte[] b = ExtractBytes(l);
            foreach (byte by in b)
                Console.Write($"{by} ");
            Array.Reverse(b);
            Assert.Equal(BitConverter.ToInt64(b, 0),l);

            int i = 1823423647;
            b = ExtractBytes(i);
            Array.Reverse(b);
            Assert.Equal(BitConverter.ToInt32(b, 0),i);

            short s = 12364;
            b = ExtractBytes(s);
            Array.Reverse(b);
            Assert.Equal(BitConverter.ToInt16(b, 0),s);
        }

        [Fact]
        public void TestConvertFromIncompleteByteArray()
        {
            int i = 1823423647;
            var b = BitConverter.GetBytes(i);
            Array.Reverse(b);
            Assert.Equal(ConvertFromIncompleteByteArray(b), i);
        }

        [Fact]
        public void TestPriorityPayload()
        {
            HTTP2Frame frame = new HTTP2Frame(1).AddPriorityPayload(true, 3, 10);
            PriorityPayload pp = frame.GetPriorityPayloadDecoded();
            Assert.True(pp.StreamDependencyIsExclusive);
            Assert.True(pp.StreamDependency == 3);
            Assert.True(pp.Weight == 10);

            frame = new HTTP2Frame(1).AddPriorityPayload(false, 4);
            pp = frame.GetPriorityPayloadDecoded();
            Assert.False(pp.StreamDependencyIsExclusive);
            Assert.True(pp.StreamDependency == 4);
            Assert.True(pp.Weight == 0);
        }

        [Fact]
        public void TestHeaderPayload()
        {
            byte[] data = { 1, 2, 3, 4 };
            HTTP2Frame frame = new HTTP2Frame(1).AddHeaderPayload(data, 2, true, true);
            HeaderPayload hh = frame.GetHeaderPayloadDecoded();
        }

    }
}
