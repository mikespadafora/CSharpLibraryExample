using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;
using BoseExSeriesLib.Enums;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void VoipAccountStatusEventHandler(object sender, VoipAccountStatusEventArgs args);

    public class VoipAccountStatusEventArgs : GenericEventArgs<VoipAccountStatusMap>
    {
        public VoipAccountStatusEventArgs()
            : base(default(VoipAccountStatusMap))
        {
        }

        public VoipAccountStatusEventArgs(VoipAccountStatusMap State)
            : base(State)
        {
        }
    }
}