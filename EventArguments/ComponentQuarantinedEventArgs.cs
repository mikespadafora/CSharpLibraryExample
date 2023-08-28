using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void ComponentQuarantinedEventHandler(object sender, ComponentQuarantinedEventArgs args);

    public class ComponentQuarantinedEventArgs : GenericEventArgs<UInt16>
    {
        public ComponentQuarantinedEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public ComponentQuarantinedEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}