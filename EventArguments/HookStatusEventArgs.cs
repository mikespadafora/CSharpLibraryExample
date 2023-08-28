using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void HookStatusEventHandler(object sender, HookStatusEventArgs args);

    public class HookStatusEventArgs : GenericEventArgs<UInt16>
    {
        public HookStatusEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public HookStatusEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}