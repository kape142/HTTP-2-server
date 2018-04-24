using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using lib.Streams;
using System.Runtime.CompilerServices;
using Http2.Hpack;

[assembly:InternalsVisibleTo("UnitTesting")]
namespace lib.HTTPObjects
{
    internal class HTTPRequestHandler
    {
        public static void SendFile(StreamHandler streamHandler, int streamId, string url)
        {
            FileInfo fi = new FileInfo(url);
            if (!fi.Exists)
            {
                SendNotFound(streamHandler, streamId);
                return;
            }
            List<HeaderField> headers = new List<HeaderField>(){
                new HeaderField{ Name = ":status", Value ="200", Sensitive = false },
                new HeaderField{ Name = "content-type", Value = Mapping.MIME_MAP[fi.Extension], Sensitive = false },
            };
            byte[] commpresedHeaders = new byte[HTTP2Frame.MaxFrameSize];
            // Encode a header block fragment into the output buffer
            var headerBlockFragment = new ArraySegment<byte>(commpresedHeaders);
            // komprimering
            var encodeResult = streamHandler.owner.hpackEncoder.EncodeInto(headerBlockFragment, headers);
            //Http2.Hpack.Encoder.Result encodeResult = Server.hPackEncoder.EncodeInto(headerBlockFragment, headers);
            commpresedHeaders = new byte[encodeResult.UsedBytes];
            // pick out the used bytes
            for (int i = 0; i < commpresedHeaders.Length; i++)
            {
                commpresedHeaders[i] = headerBlockFragment[i];
            }

            HTTP2Frame headerframe = new HTTP2Frame(streamId).AddHeaderPayload(commpresedHeaders, 0, true, false);
            streamHandler.SendFrame(headerframe);

            // send file
            using (FileStream fs = fi.OpenRead())
            using (BinaryReader reader = new BinaryReader(fs))
            {
                long length = fs.Length;
                byte[] d = new byte[HTTP2Frame.MaxFrameSize];
                for(long i = 0;  i<length-HTTP2Frame.MaxFrameSize; i+= HTTP2Frame.MaxFrameSize)
                {
                    reader.Read(d, 0, HTTP2Frame.MaxFrameSize);
                    streamHandler.SendFrame(new HTTP2Frame((int) streamId).AddDataPayload(d));
                }
                int rest = (int) length % HTTP2Frame.MaxFrameSize;
                if(rest > 0)
                {
                    reader.Read(d, 0, rest);
                    streamHandler.SendFrame(new HTTP2Frame((int)streamId).AddDataPayload(d, 0, true));
                }
            }
        }

        private static void SendNotFound(StreamHandler streamHandler, int streamId)
        {
            List<HeaderField> headers =  new List<HeaderField>(){
                new HeaderField{ Name = ":status", Value ="404", Sensitive = false }
            };
            byte[] commpresedHeaders = new byte[HTTP2Frame.MaxFrameSize];
            // Encode a header block fragment into the output buffer
            var headerBlockFragment = new ArraySegment<byte>(commpresedHeaders);
            // komprimering
            var encodeResult = streamHandler.owner.hpackEncoder.EncodeInto(headerBlockFragment, headers);
            //Http2.Hpack.Encoder.Result encodeResult = Server.hPackEncoder.EncodeInto(headerBlockFragment, headers);
            commpresedHeaders = new byte[encodeResult.UsedBytes];
            // pick out the used bytes
            for (int i = 0; i < commpresedHeaders.Length; i++)
            {
                commpresedHeaders[i] = headerBlockFragment[i];
            }

            HTTP2Frame headerframe = new HTTP2Frame(streamId).AddHeaderPayload(commpresedHeaders, 0, true, true);
            streamHandler.SendFrame(headerframe);
        }
    }
}
