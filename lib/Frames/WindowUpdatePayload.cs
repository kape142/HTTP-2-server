using System;
using System.Collections.Generic;
using System.Text;

namespace lib.Frames
{
    internal struct WindowUpdatePayload
    {
        internal int WindowSizeIncrement;

        public override string ToString()
        {
            return $"Window size increment: {WindowSizeIncrement} bytes{ConvertToLargerUnit(WindowSizeIncrement)}";
        }

        internal static string ConvertToLargerUnit(int bytes)
        {
            if(bytes >= 1073741824)
            {
                return $" = {bytes / 1073741824.0} Gb";
            }
            if(bytes>= 1048576)
            {
                return $" = {bytes / 1048576.0} Mb";
            }
            if (bytes >= 1048576)
            {
                return $" = {bytes / 1024.0} kb";
            }
            return $" = {bytes} b";
        }
    }
}
