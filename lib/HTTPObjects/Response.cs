using System;
using System.Collections.Generic;
using System.Text;

namespace lib.HTTPObjects
{
    internal class Response : IResponse
    {
        public void Send(string data)
        {
            HTTP2Frame frame = new HTTP2Frame(-1).AddDataPayload(Convert.FromBase64String(data));
            throw new NotImplementedException("jone du må fikse dette");
        }
    }
}
