using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.ProtocolSupport.Models
{
    public class ModuleSubscriptionResponseModel
    {
        public String ModuleName { get; set; }
        public String Index1 { get; set; }
        public String Index2 { get; set; }
        public String Value { get; set; }
    }
}