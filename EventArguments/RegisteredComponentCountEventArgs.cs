using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void RegisteredComponentCountEventHandler(object sender, RegisteredComponentCountEventArgs args);

    public class RegisteredComponentCountEventArgs : GenericEventArgs<UInt16>
    {
        public RegisteredComponentCountEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public RegisteredComponentCountEventArgs(UInt16 State)
            : base(State)
        {
        }
    }
}