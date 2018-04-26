using System;
using System.Collections.Generic;
using System.Text;

namespace lib.HTTPObjects
{
    public interface IResponse
    {
        void Send(string data);
    }
}
