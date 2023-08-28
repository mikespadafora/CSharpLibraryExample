using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.ProtocolSupport.Models
{
    public class ModuleRouterModels
    {
        public class RouterModel : BoseModuleModel
        {
            public RouterModel()
            {
                this.Index1 = String.Empty;
                this.Index2 = null;
            }

            public override IBoseModel GetProtocolInfo(string ModuleName, ushort OutputChannel)
            {
                this.ModuleName = ModuleName;
                this.Index1 = OutputChannel.ToString();

                return this;
            }
        }

        public class StandardCrosspointModel : BoseModuleModel
        {
            public StandardCrosspointModel()
            {
                this.Index1 = "4";
                this.Index2 = null;
            }
        }
    }
}