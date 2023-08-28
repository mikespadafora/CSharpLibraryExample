using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;
using BoseExSeriesLib.Enums;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void CallStatusEventHandler(object sender, CallStatusEventArgs args);

    public class CallStatusEventArgs : GenericEventArgs<CallStatusMap>
    {
        public CallStatusEventArgs()
            : base(default(CallStatusMap))
        {
        }

        public CallStatusEventArgs(CallStatusMap State)
            : base(State)
        {
        }
    }
}