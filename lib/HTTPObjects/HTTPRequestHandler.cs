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
                // todo returner feilmelding file not found
                return;
            }
            // Send header
            List<HeaderField> headers = new List<HeaderField>(){
                new HeaderField{ Name = ":status", Value ="200", Sensitive = false },
                new HeaderField{ Name = "content-type", Value = Mapping.MIME_MAP[fi.Extension], Sensitive = false },
            };
            byte[] commpresedHeaders = new byte[HTTP2Frame.SETTINGS_MAX_FRAME_SIZE];
            // Encode a header block fragment into the output buffer
            var headerBlockFragment = new ArraySegment<byte>(commpresedHeaders);
            // komprimering
            Http2.Hpack.Encoder encoder = new Http2.Hpack.Encoder();
            Http2.Hpack.Encoder.Result encodeResult = encoder.EncodeInto(headerBlockFragment, headers);
            //Http2.Hpack.Encoder.Result encodeResult = Server.hPackEncoder.EncodeInto(headerBlockFragment, headers);
            commpresedHeaders = new byte[encodeResult.UsedBytes];
            Console.WriteLine("Commresed bytes: " + encodeResult.UsedBytes);
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
                byte[] d = new byte[HTTP2Frame.SETTINGS_MAX_FRAME_SIZE];
                for(long i = 0;  i<length-HTTP2Frame.SETTINGS_MAX_FRAME_SIZE; i+= HTTP2Frame.SETTINGS_MAX_FRAME_SIZE)
                {
                    reader.Read(d, 0, HTTP2Frame.SETTINGS_MAX_FRAME_SIZE);
                    streamHandler.SendFrame(new HTTP2Frame((int) streamId).AddDataPayload(d));
                }
                int rest = (int) length % HTTP2Frame.SETTINGS_MAX_FRAME_SIZE;
                if(rest > 0)
                {
                    reader.Read(d, 0, rest);
                    streamHandler.SendFrame(new HTTP2Frame((int)streamId).AddDataPayload(d, 0, true));
                }
            }
        }
    }
}
