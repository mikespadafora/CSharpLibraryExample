using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.ProtocolSupport.Models
{
    public interface IBoseModel
    {
        IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel);
        String ModuleName { get; set; }
        String Index1 { get; set; }
        String Index2 { get; set; }
    }
    
    public class BoseModuleModel : IBoseModel
    {
        public String ModuleName { get; set; }
        public String Index1 { get; set; }
        public String Index2 { get; set; }

        public virtual IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = ModuleName;
            return this;
        }
    }
}