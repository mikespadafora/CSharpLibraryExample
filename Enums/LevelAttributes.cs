using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Enums
{
    public enum LevelAttributes
    {
        Unknown,
        InputVolume,
        InputGain,
        Output,
        GainModule,
        StandardMixerInput,
        StandardMixerOutput,
        PSTNInput,
        PSTNOutput,
        VoipInput,
        VoipOutput,
        UsbInput,
        UsbOutput,
        AutomixerInput,
        AutomixerOutput,
        PSTNRingLevel,
        PSTNDTMFLevel
    }
}