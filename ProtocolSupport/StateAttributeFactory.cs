using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using BoseExSeriesLib.ProtocolSupport.Models;
using BoseExSeriesLib.Enums;

namespace BoseExSeriesLib.ProtocolSupport
{
    public class StateAttributeFactory
    {
        public static IBoseModel CreateStateModel(StateAttributes attribute)
        {
            switch (attribute)
            {
                case StateAttributes.InputMute:
                    return new InputStateModel();
                case StateAttributes.OutputMute:
                    return new OutputStateModel();
                case StateAttributes.GainModuleMute:
                    return new GainModuleStateModel();
                case StateAttributes.StandardMixerInputMute:
                    return new StandardMixerInputStateModel();
                case StateAttributes.StandardMixerOutputMute:
                    return new StandardMixerOutputStateModel();
                case StateAttributes.PSTNInputMute:
                    return new PSTNInputStateModel();
                case StateAttributes.PSTNOutputMute:
                    return new PSTNOutputStateModel();
                case StateAttributes.VoipInputMute:
                    return new VoipInputStateModel();
                case StateAttributes.VoipOutputMute:
                    return new VoipOutputStateModel();
                case StateAttributes.UsbInputMute:
                    return new UsbInputStateModel();
                case StateAttributes.UsbOutputMute:
                    return new UsbOutputStateModel();
                case StateAttributes.AutomixerInputMute:
                    return new AutomixerInputStateModel();
                case StateAttributes.AutomixerOutputMute:
                    return new AutomixerOutputStateModel();
                case StateAttributes.LogicInputState:
                    return new LogicInputStateModel();
                case StateAttributes.LogicOutputState:
                    return new LogicOutputStateModel();
                default:
                    return null;
            }
        }
    }
}