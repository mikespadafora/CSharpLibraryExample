using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void InputRoutedEventHandler(object sender, InputRoutedEventArgs args);

    public class InputRoutedEventArgs : GenericEventArgs<UInt16>
    {
        public InputRoutedEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public InputRoutedEventArgs(UInt16 Value)
            : base(Value)
        {
        }
    }
}