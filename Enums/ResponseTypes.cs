using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Enums
{
    public enum ResponseTypes
    {
        ACK,
        ERROR,
        GET,
        SUBSCRIBE,
        SUBSCRIPTION,
        HEARTBEAT,
        PARAMETER_SET,
        GROUP_LEVEL_RESPONSE,
        GROUP_MUTE_RESPONSE,
        UNKNOWN
    }
}