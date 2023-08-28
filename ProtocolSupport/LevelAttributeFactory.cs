using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using BoseExSeriesLib.ProtocolSupport.Models;
using BoseExSeriesLib.Enums;

namespace BoseExSeriesLib.ProtocolSupport
{
    public class LevelAttributeFactory
    {
        public static IBoseModel CreateLevelModel(LevelAttributes attribute)
        {
            switch (attribute)
            { 
                case LevelAttributes.InputVolume:
                    return new InputVolumeModel();
                case LevelAttributes.InputGain:
                    return new InputGainModel();
                case LevelAttributes.Output:
                    return new OutputVolumeModel();
                case LevelAttributes.GainModule:
                    return new GainModuleVolumeModel();
                case LevelAttributes.StandardMixerInput:
                    return new StandardMixerInputVolumeModel();
                case LevelAttributes.StandardMixerOutput:
                    return new StandardMixerOutputVolumeModel();
                case LevelAttributes.PSTNInput:
                    return new PSTNInputVolumeModel();
                case LevelAttributes.PSTNOutput:
                    return new PSTNOutputVolumeModel();
                case LevelAttributes.VoipInput:
                    return new VoipInputVolumeModel();
                case LevelAttributes.VoipOutput:
                    return new VoipOutputVolumeModel();
                case LevelAttributes.UsbInput:
                    return new UsbInputVolumeModel();
                case LevelAttributes.UsbOutput:
                    return new UsbOutputVolumeModel();
                case LevelAttributes.AutomixerInput:
                    return new AutomixerInputVolumeModel();
                case LevelAttributes.AutomixerOutput:
                    return new AutomixerOutputVolumeModel();
                case LevelAttributes.PSTNRingLevel:
                    return new PSTNRingVolumeModel();
                case LevelAttributes.PSTNDTMFLevel:
                    return new PSTNDTMFVolumeModel();
                default:
                    return null;
            }


        }
    }
}