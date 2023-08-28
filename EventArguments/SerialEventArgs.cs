using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void SerialEventHandler(object sender, SerialEventArgs args);

    public class SerialEventArgs : GenericEventArgs<String>
    {
        public SerialEventArgs()
            : base(String.Empty)
        {
        }

        public SerialEventArgs(String Payload)
            : base(Payload)
        {
        }
    }
}