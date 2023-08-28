using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void VolumePercentageEventHandler(object sender, VolumePercentageEventArgs args);

    public class VolumePercentageEventArgs : GenericEventArgs<UInt16>
    {
        public VolumePercentageEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public VolumePercentageEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}