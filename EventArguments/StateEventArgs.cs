using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void StateChangeEventHandler(object sender, StateEventArgs args);

    public class StateEventArgs : GenericEventArgs<UInt16>
    {
        public StateEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public StateEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}