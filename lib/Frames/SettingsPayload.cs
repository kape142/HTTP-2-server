using System;
using System.Collections.Generic;
using System.Text;

namespace lib.Frames
{
    public struct SettingsPayload
    {
        public (ushort identifier, uint value)[] Settings;
    }
}
