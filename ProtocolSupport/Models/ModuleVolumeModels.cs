using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.ProtocolSupport.Models
{
    public class GainModuleVolumeModel : BoseModuleModel
    {
        public GainModuleVolumeModel()
        {
            this.Index1 = "1";
            this.Index2 = null;
        }
    }

    public class UsbInputVolumeModel : BoseModuleModel
    {
        public UsbInputVolumeModel()
        {
            this.Index2 = "1";
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = ModuleName;
            this.Index1 = Channel.ToString();

            return this;
        }
    }

    public class UsbOutputVolumeModel : BoseModuleModel
    {
        public UsbOutputVolumeModel()
        {
            this.Index2 = "1";
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = ModuleName;
            this.Index1 = Channel.ToString();

            return this;
        }
    }

    public class InputVolumeModel : BoseModuleModel
    {
        public InputVolumeModel()
        {
            this.Index1 = "3";
            this.Index2 = null;
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = String.Format("Input {0}", Channel);
            
            return this;
        }
    }

    public class InputGainModel : BoseModuleModel
    {
        public InputGainModel()
        {
            this.Index1 = "2";
            this.Index2 = null;
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = String.Format("Input {0}", Channel);

            return this;
        }
    }

    public class OutputVolumeModel : BoseModuleModel
    {
        public OutputVolumeModel()
        {
            this.Index1 = "1";
            this.Index2 = null;
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = String.Format("Output {0}", Channel);

            return this;
        }
    }

    public class StandardMixerInputVolumeModel : BoseModuleModel
    {
        public StandardMixerInputVolumeModel()
        {
            this.Index1 = "1";
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = ModuleName;

            if (Channel == 1)
                this.Index2 = Channel.ToString();
            else
                this.Index2 = ((Channel * 2) - 1).ToString();

            return this;
        }
    }

    public class StandardMixerOutputVolumeModel : BoseModuleModel
    {
        public StandardMixerOutputVolumeModel()
        {
            this.Index1 = "2";
        }

        public override IBoseModel GetProtocolInfo(String ModuleName, UInt16 Channel)
        {
            this.ModuleName = ModuleName;

            this.Index2 = Channel == 1 ? Channel.ToString() : ((Channel * 2) - 1).ToString();

            return this;
        }
    }

    public class PSTNInputVolumeModel : BoseModuleModel
    {
        public PSTNInputVolumeModel()
        {
            this.Index1 = "1";
            this.Index2 = "1";
        }
    }

    public class PSTNOutputVolumeModel : BoseModuleModel
    {
        public PSTNOutputVolumeModel()
        {
            this.Index1 = "1";
            this.Index2 = null;
        }
    }

    public class VoipInputVolumeModel : BoseModuleModel
    {
        public VoipInputVolumeModel()
        {
            this.Index1 = "1";
            this.Index2 = "1";
        }
    }

    public class VoipOutputVolumeModel : BoseModuleModel
    {
        public VoipOutputVolumeModel()
        {
            this.Index1 = "1";
            this.Index2 = null;
        }
    }

    public class AutomixerInputVolumeModel : BoseModuleModel
    {
        public AutomixerInputVolumeModel()
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

    public class AutomixerOutputVolumeModel : BoseModuleModel
    {
        public AutomixerOutputVolumeModel()
        {
            this.Index1 = "0";
            this.Index2 = "1";
        }
    }

    public class PSTNRingVolumeModel : BoseModuleModel
    {
        public PSTNRingVolumeModel()
        {
            this.Index1 = "0";
            this.Index2 = "3";
        }
    }

    public class PSTNDTMFVolumeModel : BoseModuleModel
    {
        public PSTNDTMFVolumeModel()
        {
            this.Index1 = "0";
            this.Index2 = "4";
        }
    }
}