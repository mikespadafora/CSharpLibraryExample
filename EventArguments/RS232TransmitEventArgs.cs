using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void RS232TransmitEventHandler(object sender, RS232TransmitEventArgs args);

    public class RS232TransmitEventArgs : GenericEventArgs<String>
    {
        public RS232TransmitEventArgs()
            : base(String.Empty)
        {
        }

        public RS232TransmitEventArgs(String Payload)
            : base(Payload)
        {
        }
    }
}