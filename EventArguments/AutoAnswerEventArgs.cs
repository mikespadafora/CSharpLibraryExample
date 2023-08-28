using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void AutoAnswerEventHandler(object sender, AutoAnswerEventArgs args);

    public class AutoAnswerEventArgs : GenericEventArgs<UInt16>
    {
        public AutoAnswerEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public AutoAnswerEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}