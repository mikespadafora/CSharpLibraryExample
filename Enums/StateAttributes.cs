using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Enums
{
    public enum StateAttributes
    {
        Unknown,
        InputMute,
        OutputMute,
        GainModuleMute,
        StandardMixerInputMute,
        StandardMixerOutputMute,
        PSTNInputMute,
        PSTNOutputMute,
        VoipInputMute,
        VoipOutputMute,
        UsbInputMute,
        UsbOutputMute,
        AutomixerInputMute,
        AutomixerOutputMute,
        LogicInputState,
        LogicOutputState
    }
}