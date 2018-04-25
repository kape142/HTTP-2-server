using System;
using System.Collections.Generic;
using System.Text;

namespace lib.HTTPObjects
{
    class Request
    {
        public Dictionary<string , string> Params { get; } = new Dictionary<string, string>();

        internal void AddParams(params (string key,string value)[] list)
        {
            foreach((string key, string value) in list)
                Params.Add(key, value);
        }
    }
}
