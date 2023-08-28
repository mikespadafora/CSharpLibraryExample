using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using System.ComponentModel;
using BoseExSeriesLib.Components;
using BoseExSeriesLib.Enums;

namespace BoseExSeriesLib.ProtocolSupport
{
    public class ScaleUtil
    {
        public static Int16 GetNextLevel(LevelComponent component, RampTypes RampType, Int16 CurrentLevel)
        {
            if (component is LevelComponent)
            {
                if (component.ModuleType != LevelAttributes.InputGain)
                {
                    if (RampType == RampTypes.UP)
                    {
                        if (CurrentLevel + component.LevelStep <= component.UpperLimit)
                            return (short)(CurrentLevel + component.LevelStep);
                        else
                            return component.UpperLimit;
                    }
                    else if (RampType == RampTypes.DOWN)
                    {
                        if (CurrentLevel - component.LevelStep >= component.LowerLimit)
                            return (short)(CurrentLevel - component.LevelStep);
                        else
                            return component.LowerLimit;
                    }
                }
                else
                {
                    Int16[] values = { 0, 14, 24, 32, 44, 54, 64 };
                    int currentIndex = 0;


                    if (values.Contains(CurrentLevel))
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            if (values[i] == CurrentLevel)
                                currentIndex = i;
                        }

                        if (RampType == RampTypes.UP && currentIndex < values.Length)
                            return values[currentIndex + 1];
                        else if (RampType == RampTypes.DOWN && currentIndex > 0)
                            return values[currentIndex - 1];
                    }
                }
            }

            return CurrentLevel;
        }

        public static Int16 GetNextLevel(Int16 CurrentLevel, RampTypes RampType, Int16 UpperLimit, Int16 LowerLimit, UInt16 LevelStep)
        {
            if (RampType == RampTypes.UP)
            {
                if (CurrentLevel + LevelStep <= UpperLimit)
                    return (short)(CurrentLevel + LevelStep);
                else
                    return UpperLimit;
            }
            else if (RampType == RampTypes.DOWN)
            {
                if (CurrentLevel - LevelStep >= LowerLimit)
                    return (short)(CurrentLevel - LevelStep);
                else
                    return LowerLimit;
            }

            return CurrentLevel;
        }

        public static UInt16 ConvertToPercentage(LevelComponent component, Int16 CurrentLevel)
        {
            Int16 Stage1 = (Int16)(CurrentLevel - component.LowerLimit);
            Int16 Stage2 = (Int16)(component.UpperLimit - component.LowerLimit);

            return (UInt16)((Stage1 * UInt16.MaxValue) / Stage2);
        }

        public static UInt16 ConvertToPercentage(Int16 CurrentLevel, Int16 UpperLimit, Int16 LowerLimit)
        {
            Int16 Stage1 = (Int16)(CurrentLevel - LowerLimit);
            Int16 Stage2 = (Int16)(UpperLimit - LowerLimit);

            return (UInt16)((Stage1 * UInt16.MaxValue) / Stage2);
        }


        public static Int16 ConvertFromPercentage(LevelComponent component, UInt16 CurrentPercentage)
        {
            if (component.ModuleType != LevelAttributes.InputGain)
            {
                Int16 Stage1 = (Int16)(component.UpperLimit - component.LowerLimit);
                double Stage2 = (double)UInt16.MaxValue / Stage1;

                return (Int16)((CurrentPercentage / Stage2) + component.LowerLimit);
            }
            else
            {
                Int16[] values = { 0, 14, 24, 32, 44, 54, 64 };

                double scale = (double)UInt16.MaxValue / (values.Length - 1);
                short index = (short)(CurrentPercentage / scale);

                return values[index];
            }
        }

        public static Int16 ConvertFromPercentage(UInt16 CurrentPercentage, Int16 UpperLimit, Int16 LowerLimit)
        {
            Int16 Stage1 = (Int16)(UpperLimit - LowerLimit);
            double Stage2 = (double)UInt16.MaxValue / Stage1;

            return (Int16)((CurrentPercentage / Stage2) + LowerLimit);
        }
    }
}