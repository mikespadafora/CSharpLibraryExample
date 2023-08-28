using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void CallActiveEventHandler(object sender, CallActiveEventArgs args);

    public class CallActiveEventArgs : GenericEventArgs<UInt16>
    {
        public CallActiveEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public CallActiveEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}