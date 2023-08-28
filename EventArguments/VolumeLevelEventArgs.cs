using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void VolumeLevelEventHandler(object sender, VolumeLevelEventArgs args);

    public class VolumeLevelEventArgs : GenericEventArgs<Int16>
    {
        public VolumeLevelEventArgs()
            : base(Int16.MinValue)
        {
        }

        public VolumeLevelEventArgs(Int16 State)
            : base(State)
        {
        }
    }
}