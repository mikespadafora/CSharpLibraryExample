using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void CrosspointRoutedEventHandler(object sender, CrosspointRoutedEventArgs args);

    public class CrosspointRoutedEventArgs : GenericEventArgs<UInt32>
    {
        public CrosspointRoutedEventArgs()
            : base(UInt32.MinValue)
        {
        }

        public CrosspointRoutedEventArgs(UInt32 Value)
            : base(Value)
        {
        }
    }
}