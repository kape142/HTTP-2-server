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
        public static readonly HeaderField HEADER_OK = new HeaderField{Name = ":status", Value = "200", Sensitive = false };
        public static readonly HeaderField HEADER_NOTFOUND = new HeaderField{Name = ":status", Value = "404", Sensitive = false };
        public static readonly HeaderField HEADER_METHODNOTALLOWED = new HeaderField{Name = ":status", Value = "405", Sensitive = false };
        public static readonly HeaderField HEADER_INTERNALSERVERERROR = new HeaderField{Name = ":status", Value = "500", Sensitive = false };

        public static void SendFile(StreamHandler streamHandler, int streamId, string url)
        {
            FileInfo fi = new FileInfo(url);
            if (!fi.Exists)
            {
                SendNotFound(streamHandler, streamId);
                return;
            }
            List<HeaderField> headers = new List<HeaderField>(){
                HEADER_OK,
                new HeaderField{ Name = "content-type", Value = Mapping.MIME_MAP[fi.Extension], Sensitive = false },
                new HeaderField{ Name = "Content-Encoding", Value = "gzip", Sensitive = false },
            };
            byte[] commpresedHeaders = new byte[HTTP2Frame.SETTINGS_MAX_FRAME_SIZE];
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
            fi = ZipStream.Compress(fi);
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
                    d = new byte[rest];
                    reader.Read(d, 0, rest);
                    streamHandler.SendFrame(new HTTP2Frame((int)streamId).AddDataPayload(d, 0, true));
                }
            }
        }

        public static void SendData(StreamHandler streamHandler, int streamId, byte[] data, string contentType)
        {
            if (data.Length < 1)
            {
                SendNotFound(streamHandler, streamId);
                return;
            }
            List<HeaderField> headers = new List<HeaderField>(){
                HEADER_OK,
                new HeaderField{ Name = "content-type", Value = contentType, Sensitive = false },
            };
            byte[] commpresedHeaders = new byte[HTTP2Frame.SETTINGS_MAX_FRAME_SIZE];
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

            // send data
            try
            {
                long length = data.Length;
                long sent = 0;
                byte[] d = new byte[HTTP2Frame.SETTINGS_MAX_FRAME_SIZE];
                
                while (sent < length)
                {
                    for (long j = 0; (j < HTTP2Frame.SETTINGS_MAX_FRAME_SIZE || sent <= length); j++)
                    {
                        d[j] = data[sent++];
                    }
                    streamHandler.SendFrame(new HTTP2Frame((int)streamId).AddDataPayload(d));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendData()\n" + ex);
            }
        }

        private static void SendNotInternalServerError(StreamHandler streamHandler, int streamId)
        {
            List<HeaderField> headers = new List<HeaderField>() { HEADER_INTERNALSERVERERROR };
            SendHeader(streamHandler, streamId, headers);
        }

        private static void SendNotFound(StreamHandler streamHandler, int streamId)
        {
            List<HeaderField> headers = new List<HeaderField>(){ HEADER_NOTFOUND };
            SendHeader(streamHandler, streamId, headers);
        }

        public static void SendMethodNotAllowed(StreamHandler streamHandler, int streamId)
        {
            List<HeaderField> headers = new List<HeaderField>() { HEADER_METHODNOTALLOWED };
            SendHeader(streamHandler, streamId, headers);
        }

        public static void SendOk(StreamHandler streamHandler, int streamId, List<HeaderField> aditionalHeaders = null, bool endStream = false)
        {
            List<HeaderField> headers = new List<HeaderField>() { HEADER_OK};
            if (aditionalHeaders != null) headers.AddRange(aditionalHeaders);
            SendHeader(streamHandler, streamId, headers, true, endStream);
        }

        private static void SendHeader(StreamHandler streamHandler, int streamId, List<HeaderField> headers, bool endheaders = true, bool endStream = true)
        {
            byte[] commpresedHeaders = new byte[HTTP2Frame.SETTINGS_MAX_FRAME_SIZE];
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

            HTTP2Frame headerframe = new HTTP2Frame(streamId).AddHeaderPayload(commpresedHeaders, 0, endheaders, endheaders);
            streamHandler.SendFrame(headerframe); // todo støtte for datautvidelse med contuation rammer
        }



    }
}
