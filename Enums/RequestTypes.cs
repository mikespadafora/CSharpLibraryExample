using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Enums
{
    public enum RequestType
    {
        Subscribe,
        Unsubscribe,
        Set,
        ParameterSetRequest,
        Get,
        Heartbeat,
        Action,
        GroupLevelSet,
        GroupMuteSet,
        Unknown
    }
}