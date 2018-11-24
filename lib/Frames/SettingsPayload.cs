using System;
using System.Collections.Generic;
using System.Text;
using static lib.HTTPObjects.HTTP2Frame;

namespace lib.Frames
{
    public struct SettingsPayload
    {
        public (ushort identifier, uint value)[] Settings;

        public override string ToString()
        {
            string ret = "Settings:\n";
            foreach (var item in Settings)
            {
                ret += GetSettingsName(item.identifier) + " " + item.value + "\n";
            }
            return ret;
        }

        private string GetSettingsName(int nr)
        {
            switch (nr)
            {
                case SETTINGS_HEADER_TABLE_SIZE:
                    return "Max header table size";
                case SETTINGS_ENABLE_PUSH:
                    return "Enable push";
                case SETTINGS_MAX_CONCURRENT_STREAMS:
                    return "Max concurrent streams";
                case SETTINGS_INITIAL_WINDOW_SIZE:
                    return "Initial window size";
                case SETTINGS_MAX_FRAME_SIZE:
                    return "Max frame size";
                case SETTINGS_MAX_HEADER_LIST_SIZE:
                    return "Max header list size";
                default:
                    return "Unknown setting";
            }
        }
    }
}
