using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void InitializationEventHandler(object sender, InitializationEventArgs args);

    public class InitializationEventArgs : GenericEventArgs<UInt16>
    {
        public InitializationEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public InitializationEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}