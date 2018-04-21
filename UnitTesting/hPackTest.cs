using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Http2.Hpack;

namespace UnitTesting
{
    public class hPackTest
    {
        const int MaxFrameSize = 65535;
        [Fact]
        public void TestHPack()
        {
            Http2.Hpack.Decoder decoder = new Http2.Hpack.Decoder();
            Http2.Hpack.Encoder encoder = new Http2.Hpack.Encoder();

            byte[] buffer = new byte[10];
            List<HeaderField> headers = new List<HeaderField>(){
                new HeaderField{ Name = ":status", Value ="307", Sensitive = false },
                new HeaderField{ Name = "cache-control", Value ="private", Sensitive = false },
                new HeaderField{ Name = "date", Value ="Mon, 21 Oct 2013 20:13:21 GMT", Sensitive = false },
                new HeaderField{ Name = "location", Value ="https://www.example.com", Sensitive = false },
            };
            byte[] outBuf = new byte[100];


            // Encode a header block fragment into the output buffer
            var headerBlockFragment = new ArraySegment<byte>(outBuf);
            // komprimering
            Http2.Hpack.Encoder.Result encodeResult = encoder.EncodeInto(headerBlockFragment, headers);
            

            uint maxHeaderFieldsSize = 100;

            Http2.Hpack.DecoderExtensions.DecodeFragmentResult decodeResult = decoder.DecodeHeaderBlockFragment(
                new ArraySegment<byte>(buffer, 0, buffer.Length), maxHeaderFieldsSize, headers);
            foreach (var item in headers)
            {
                Console.WriteLine(item.Name + " " + item.Value);
            }

           
        }
    }
}
