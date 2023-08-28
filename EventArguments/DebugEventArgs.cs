using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void DebugEventHandler(object sender, DebugEventArgs args);

    public class DebugEventArgs : GenericEventArgs<String>
    {
        public DebugEventArgs()
            : base(String.Empty)
        {
        }

        public DebugEventArgs(String Payload)
            : base(Payload)
        {
        }
    }
}