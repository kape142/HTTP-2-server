using System;
using System.Collections.Generic;
using System.Text;

namespace lib.HTTPObjects
{
    public class Request
    {

        private byte[] _data;

        public string ContentType{ get; private set; }

        internal Request() { }

        internal Request(byte[] data, string contentType)
        {
            _data = data;
            ContentType = contentType;
        }

        public string BodyAsString()
        {
            return Encoding.ASCII.GetString(_data);
        }

        public byte[] BodyAsBytes()
        {
            return _data;
        }

        public Dictionary<string , string> Params { get; } = new Dictionary<string, string>();

        internal void AddParams(params (string key,string value)[] list)
        {
            foreach((string key, string value) in list)
                Params.Add(key, value);
        }
    }
}
