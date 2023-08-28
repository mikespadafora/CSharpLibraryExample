using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using BoseExSeriesLib.ProtocolSupport.Models;
using CCI.SimplSharp.Library.Components.States;
using BoseExSeriesLib.ProtocolSupport;
using BoseExSeriesLib.Enums;
using BoseExSeriesLib.EventArguments;
using System.Text.RegularExpressions;
using CCI.SimplSharp.Library.IO.Common;
using CCI.SimplSharp.Library.IO.Utilities;

namespace BoseExSeriesLib.Components
{
    public class PSTNDialerComponent : ADialerComponent
    {

        // Events
        ////////////////////////////////////////////////////

        public event HookStatusEventHandler OnHookStatusChange;
        public event VolumeLevelEventHandler OnRingVolumeChange;
        public event VolumeLevelEventHandler OnDTMFVolumeChange;

        // Protocol
        ////////////////////////////////////////////////////

        protected IBoseModel CountryCodeInfo = null;
        protected IBoseModel ManualHookInfo = null;

        // Properties
        ////////////////////////////////////////////////////

        private UInt16 countryCode { get; set; }

        private Boolean _hookState
        {
            get
            {
                if (this.ManualHookState.IsInitialized())
                    return (bool)this.ManualHookState.State;
                else
                    return false;
            }
        }

        // Members
        ////////////////////////////////////////////////////

        private LevelComponent RingLevel = null;
        private LevelComponent DTMFLevel = null;

        // Component States
        ////////////////////////////////////////////////////

        protected BooleanComponentState ManualHookState = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public PSTNDialerComponent()
            : base() 
        {
            this.ManualHookState = new BooleanComponentState();
            this.ManualHookState.OnProcessUpdate += new CCI.SimplSharp.Library.Components.EventArguments.BooleanEventHandler(ManualHookState_OnProcessUpdate);
            this.States.Add(this.ManualHookState);

            this.RingLevel = new LevelComponent();
            this.DTMFLevel = new LevelComponent();

            this.RingLevel.OnVolumeLevelChange += new VolumeLevelEventHandler(RingLevel_OnVolumeLevelChange);
            this.DTMFLevel.OnVolumeLevelChange += new VolumeLevelEventHandler(DTMFLevel_OnVolumeLevelChange);
        }

        // Exposed
        ////////////////////////////////////////////////////

        public override void Configure(ushort CommandProcessorId, string ModuleName, UInt16 CountryCode)
        {
            this.RingLevel.Configure(CommandProcessorId, ModuleName, (ushort)LevelAttributes.PSTNRingLevel, 0, 10, -30, 1);
            this.DTMFLevel.Configure(CommandProcessorId, ModuleName, (ushort)LevelAttributes.PSTNDTMFLevel, 0, 10, -20, 1);

            this.countryCode = CountryCode;

            this.CallStatusInfo = new BoseModuleModel();
            this.CallStatusInfo.ModuleName = ModuleName;
            this.CallStatusInfo.Index1 = "0";
            this.CallStatusInfo.Index2 = "1";

            this.CallerIdInfo = new BoseModuleModel();
            this.CallerIdInfo.ModuleName = ModuleName;
            this.CallerIdInfo.Index1 = "0";
            this.CallerIdInfo.Index2 = "2";

            this.AutoAnswerInfo = new BoseModuleModel();
            this.AutoAnswerInfo.ModuleName = ModuleName;
            this.AutoAnswerInfo.Index1 = "0";
            this.AutoAnswerInfo.Index2 = "6";

            this.CountryCodeInfo = new BoseModuleModel();
            this.CountryCodeInfo.ModuleName = ModuleName;
            this.CountryCodeInfo.Index1 = "0";
            this.CountryCodeInfo.Index2 = "7";

            this.CallActiveInfo = new BoseModuleModel();
            this.CallActiveInfo.ModuleName = ModuleName;
            this.CallActiveInfo.Index1 = "0";
            this.CallActiveInfo.Index2 = "8";

            this.ManualHookInfo = new BoseModuleModel();
            this.ManualHookInfo.ModuleName = ModuleName;
            this.ManualHookInfo.Index1 = "0";
            this.ManualHookInfo.Index2 = "9";

            this.DialKeyInfo = new BoseModuleModel();
            this.DialKeyInfo.ModuleName = ModuleName;
            this.DialKeyInfo.Index1 = "1";

            this.MakeCallInfo = new BoseModuleModel();
            this.MakeCallInfo.ModuleName = ModuleName;
            this.MakeCallInfo.Index1 = "2";

            this.EndCallInfo = new BoseModuleModel();
            this.EndCallInfo.ModuleName = ModuleName;
            this.EndCallInfo.Index1 = "3";

            this.AnswerCallInfo = new BoseModuleModel();
            this.AnswerCallInfo.ModuleName = ModuleName;
            this.AnswerCallInfo.Index1 = "4";

            base.Configure(CommandProcessorId, ModuleName, CountryCode);
        }

        public void HookStatusOn()
        {
            if (this.IsInitialized)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.ManualHookInfo, StateValues.On.Value));
        }

        public void HookStatusOff()
        {
            if (this.IsInitialized)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.ManualHookInfo, StateValues.Off.Value));
        }

        public void HookStatusToggle()
        {
            if (this.IsInitialized)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.ManualHookInfo, StateValues.Toggle.Value));
        }

        public void Flash()
        {
            if (this._callActive)
                this.DialDigit("!");
        }

        public void RingLevelRaise()
        {
            this.RingLevel.Raise();
        }

        public void RingLevelLower()
        {
            this.RingLevel.Lower();
        }

        public void RingLevelStop()
        {
            this.RingLevel.Stop();
        }

        public void SetRingLevel(Int16 level)
        {
            this.RingLevel.SetVolumeLevel(level);
        }

        public void DTMFLevelRaise()
        {
            this.DTMFLevel.Raise();
        }

        public void DTMFLevelLower()
        {
            this.DTMFLevel.Lower();
        }

        public void DTMFLevelStop()
        {
            this.DTMFLevel.Stop();
        }

        public void SetDTMFLevel(Int16 level)
        {
            this.DTMFLevel.SetVolumeLevel(level);
        }


        // Helper Methods
        ////////////////////////////////////////////////////

        private void processHookStatus(String state)
        {
            try
            {
                string value = state == StateValues.On.Value ? "1" : "0";

                this.ManualHookState.UpdateState(value);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine(ex.Message);
            }        
        }

        private void processCallStatus(String status)
        {
            string pattern = @"[^\""]+";

            Match m = Regex.Match(status, pattern);

            if (m.Success)
                this.CallStatusState.UpdateState(m.Value);
        }

        private void processCallerId(String status)
        {
            
            if (status.Contains("\""))
                status = status.Replace("\"", String.Empty);

            if (status.Contains("&"))
            {
                string[] arr = Regex.Split(status, "&");

                this.callerId.Number = (String.IsNullOrEmpty(arr[1]) == false) ? arr[1].Trim() : String.Empty;
                this.callerId.Name = (String.IsNullOrEmpty(arr[2]) == false) ? arr[2].Trim() : String.Empty;
            }
            else
                this.callerId.Clear();

            this.ProcessCallerId();
        }

        private void processCallActiveState(String state)
        {
            try
            {
                string value = state == StateValues.On.Value ? "1" : "0";

                this.CallActiveState.UpdateState(value);
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine(ex.Message);
            }
        }

        private void processAutoAnswerState(String state)
        {
            try
            {
                ushort value = UInt16.Parse(state);

                if (value > 0)
                {
                    this.AutoAnswerState.UpdateState("1");
                    this.AutoAnswerRingCount = value;
                }
                else
                    this.AutoAnswerState.UpdateState("0");
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine(ex.Message);
            }
        }


        public override void Poll()
        {
            //
        }

        public override void ProcessSubscription(BoseExSeriesLib.ProtocolSupport.IResponse response)
        {
            if (response is IResponse)
            {
                if (response is ModuleSubscriptionResponse)
                {
                    ModuleSubscriptionResponse r = (ModuleSubscriptionResponse)response;

                    if (r.Response.ModuleName == this.ModuleName && r.Response.Index1 == "0")
                    {
                        switch (r.Response.Index2)
                        { 
                            case "1": // Call State Response

                                this.processCallStatus(r.Response.Value);
                                break;

                            case "2": // Caller ID Response
                                this.processCallerId(r.Response.Value);
                                break;

                            case "6": // Auto Answer Response
                                this.processAutoAnswerState(r.Response.Value);
                                break;

                            case "8": // Call Active State Response
                                this.processCallActiveState(r.Response.Value);
                                break;

                            case "9": // Hook Status Response
                                this.processHookStatus(r.Response.Value);
                                break;
                        }
                    }
                }
            }
        }

        public override void ProcessResponse(IResponse response, MessageBundle request)
        {
            if (response is IResponse)
            {
                if (response is SubscriptionRequestResponse)
                {
                    this.subscribed = ((SubscriptionRequestResponse)response).Subscribed;
                }
                else if (response is ErrorResponse)
                {
                    ErrorResponse r = (ErrorResponse)response;

                    if (r.Error.ErrorCode == ErrorResponses.INVALID_MODULE_NAME.ErrorCode || r.Error.ErrorCode == ErrorResponses.ILLEGAL_INDEX.ErrorCode)
                        this.processQuarantineChange(true);
                }
            }
        }

        public override void Refresh()
        {
            if (this.OnHookStatusChange != null)
                this.OnHookStatusChange(this, new HookStatusEventArgs(CrestronSimplPlusHelper.ToCrestronBool(this._hookState)));

            base.Refresh();
        }

        public override void Reinitialize()
        {           
            base.Reinitialize();
        }

        protected override void Subscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.CountryCodeInfo, this.countryCode.ToString()));
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSubscribeMessage(this, this.ManualHookInfo));
            }

            base.Subscribe();
        }

        protected override void Unsubscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleUnsubscribeMessage(this, this.ManualHookInfo));
            }

            base.Unsubscribe();
        }

        // Component States Events
        ////////////////////////////////////////////////////

        void ManualHookState_OnProcessUpdate(object sender, CCI.SimplSharp.Library.Components.EventArguments.BooleanEventArgs args)
        {
            if (this.OnHookStatusChange != null)
                this.OnHookStatusChange(this, new HookStatusEventArgs(CrestronSimplPlusHelper.ToCrestronBool(args.Payload)));

            this.updateInitializationChange();
        }

        // Volume Level Events
        ////////////////////////////////////////////////////

        void DTMFLevel_OnVolumeLevelChange(object sender, VolumeLevelEventArgs args)
        {
            if (this.OnDTMFVolumeChange != null)
                this.OnDTMFVolumeChange(this, new VolumeLevelEventArgs(args.Payload));
        }

        void RingLevel_OnVolumeLevelChange(object sender, VolumeLevelEventArgs args)
        {
            if (this.OnRingVolumeChange != null)
                this.OnRingVolumeChange(this, new VolumeLevelEventArgs(args.Payload));
        }


        // Dispose
        ////////////////////////////////////////////////////

        public override void Dispose()
        {
            if (this.RingLevel is LevelComponent)
            {
                ((IDisposable)this.RingLevel).Dispose();
                this.RingLevel = null;
            }

            if (this.DTMFLevel is LevelComponent)
            {
                ((IDisposable)this.RingLevel).Dispose();
                this.DTMFLevel = null;
            }
            
            base.Dispose();
        }
    }
}