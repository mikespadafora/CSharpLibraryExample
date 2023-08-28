using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void CommunicatingEventHandler(object sender, CommunicatingEventArgs args);

    public class CommunicatingEventArgs : GenericEventArgs<UInt16>
    {
        public CommunicatingEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public CommunicatingEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}