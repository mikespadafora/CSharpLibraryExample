using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void LastParameterSetEventHandler(object sender, LastParameterSetEventArgs args);

    public class LastParameterSetEventArgs : GenericEventArgs<UInt16>
    {
        public LastParameterSetEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public LastParameterSetEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}