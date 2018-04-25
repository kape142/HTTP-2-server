using System;
using System.Collections.Generic;
using System.Text;

namespace lib.HTTPObjects
{
    interface IResponse
    {
        void Send(string data);
    }
}
