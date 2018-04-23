using System;
using System.Collections.Generic;
using System.Text;

namespace lib.Frames
{
    class GoAwayPayload
    {
        public bool r;
        public int LastStreamId;
        public int ErrorCode;
        public string AdditionalDebugData;

        public override string ToString()
        {
            return $"GO_AWAY: \nLast-stream-ID: {LastStreamId} \nErrorCode: {ErrorCode} \nAdditionalDebugData: {AdditionalDebugData}";
        }
    }
}
