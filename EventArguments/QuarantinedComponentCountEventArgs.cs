using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void QuarantinedComponentCountEventHandler(object sender, QuarantinedComponentCountEventArgs args);

    public class QuarantinedComponentCountEventArgs : GenericEventArgs<UInt16>
    {
        public QuarantinedComponentCountEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public QuarantinedComponentCountEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}