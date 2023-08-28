using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.ProtocolSupport.Models
{
    public class GainModuleStateModel : BoseModuleModel
    {
        public GainModuleStateModel()
        {
            this.Index1 = "2";
            this.Index2 = null;
        }
    }

    public class UsbInputStateModel : BoseModuleModel
    {
        public UsbInputStateModel()
        {
            this.Index2 = "2";
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = ModuleName;
            this.Index1 = Channel.ToString();

            return this;
        }
    }

    public class UsbOutputStateModel : BoseModuleModel
    {
        public UsbOutputStateModel()
        {
            this.Index2 = "2";
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = ModuleName;
            this.Index1 = Channel.ToString();

            return this;
        }
    }

    public class InputStateModel : BoseModuleModel
    {
        public InputStateModel()
        {
            this.Index1 = "4";
            this.Index2 = null;
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = String.Format("Input {0}", Channel);

            return this;
        }
    }

    public class OutputStateModel : BoseModuleModel
    {
        public OutputStateModel()
        {
            this.Index1 = "2";
            this.Index2 = null;
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = String.Format("Output {0}", Channel);

            return this;
        }
    }

    public class StandardMixerInputStateModel : BoseModuleModel
    {
        public StandardMixerInputStateModel()
        {
            this.Index1 = "1";
        }

        public override IBoseModel GetProtocolInfo(string ModuleName, ushort Channel)
        {
            this.ModuleName = ModuleName;

            this.Index2 = Channel == 1 ? "2" : (Channel * 2).ToString();

            return this;
        }
    }

    public class StandardMixerOutputStateModel : BoseModuleModel
    {
        public StandardMixerOutputStateModel()
        {
            this.Index1 = "2";
        }

        public override IBoseModel GetProtocolInfo(string ModuleName, ushort Channel)
        {
            this.ModuleName = ModuleName;

            this.Index2 = Channel == 1 ? "2" : (Channel * 2).ToString();

            return this;
        }
    }

    public class PSTNInputStateModel : BoseModuleModel
    {
        public PSTNInputStateModel()
        {
            this.Index1 = "1";
            this.Index2 = "2";
        }
    }

    public class PSTNOutputStateModel : BoseModuleModel
    {
        public PSTNOutputStateModel()
        {
            this.Index1 = "2";
            this.Index2 = null;
        }
    }

    public class VoipInputStateModel : BoseModuleModel
    {
        public VoipInputStateModel()
        {
            this.Index1 = "1";
            this.Index2 = "2";
        }
    }

    public class VoipOutputStateModel : BoseModuleModel
    {
        public VoipOutputStateModel()
        {
            this.Index1 = "2";
            this.Index2 = null;
        }
    }

    public class AutomixerInputStateModel : BoseModuleModel
    {
        public AutomixerInputStateModel()
        {
            this.Index2 = "2";
        }

        public override IBoseModel GetProtocolInfo(string ModuleName, ushort Channel)
        {
            this.ModuleName = ModuleName;
            this.Index1 = Channel.ToString();

            return this;
        }
    }

    public class AutomixerOutputStateModel : BoseModuleModel
    {
        public AutomixerOutputStateModel()
        {
            this.Index1 = "0";
            this.Index2 = "2";
        }
    }

    public class LogicInputStateModel : BoseModuleModel
    {
        public LogicInputStateModel()
        {
            this.Index2 = "1";
        }

        public override IBoseModel GetProtocolInfo(string ModuleName, ushort Channel)
        {
            this.ModuleName = ModuleName;
            this.Index1 = Channel.ToString();

            return this;
        }
    }

    public class LogicOutputStateModel : BoseModuleModel
    {
        public LogicOutputStateModel()
        {
            this.Index2 = "1";
        }

        public override IBoseModel GetProtocolInfo(string ModuleName, ushort Channel)
        {
            this.ModuleName = ModuleName;
            this.Index1 = Channel.ToString();

            return this;
        }
    }
}