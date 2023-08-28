using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void KeypadTextEventHandler(object sender, KeypadTextEventArgs args);

    public class KeypadTextEventArgs : GenericEventArgs<String>
    {
        public KeypadTextEventArgs()
            : base(String.Empty)
        {
        }

        public KeypadTextEventArgs(String Payload)
            : base(Payload)
        {
        }
    }
}