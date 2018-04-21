using System;
using Xunit;
using lib;
using lib.HTTPObjects;

namespace UnitTest
{
    public class HTTP2FrameTest
    {
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

        
    }
}
