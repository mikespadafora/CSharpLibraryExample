using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.EventArguments;
using BoseExSeriesLib.Enums;

namespace BoseExSeriesLib.EventArguments
{
    public delegate void CallerIdEventHandler(object sender, CallerIdEventArgs args);

    public class CallerIdEventArgs : GenericEventArgs<CallerIdInfo>
    {
        public CallerIdEventArgs()
            : base(default(CallerIdInfo))
        {
        }

        public CallerIdEventArgs(CallerIdInfo State)
            : base(State)
        {
        }
    }

    public class CallerIdInfo
    {
        public String Name { get; set; }
        public String Number { get; set; }

        public CallerIdInfo()
        {
            this.Name = String.Empty;
            this.Number = String.Empty;
        }

        public void Clear()
        {
            this.Name = String.Empty;
            this.Number = String.Empty;
        }
    }
}