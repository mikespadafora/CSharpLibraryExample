using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void SourceSelectionEventHandler(object sender, SourceSelectionEventArgs args);

    public class SourceSelectionEventArgs : GenericEventArgs<UInt16>
    {
        public SourceSelectionEventArgs()
            : base(UInt16.MinValue)
        {
        }

        public SourceSelectionEventArgs(UInt16 Value)
            : base(Value)
        {
        }
    }
}