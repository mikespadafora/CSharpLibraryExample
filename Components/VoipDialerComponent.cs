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

namespace BoseExSeriesLib.Components
{
    public class VoipDialerComponent : ADialerComponent
    {

        // Events
        ////////////////////////////////////////////////////

        public event VoipAccountStatusEventHandler OnVoipAccountStatusChange;

        // Protocol
        ////////////////////////////////////////////////////

        protected IBoseModel VoipAccountStatusInfo = null;
        protected IBoseModel TransferCallInfo = null;

        // Members
        ////////////////////////////////////////////////////

        private VoipAccountStatusMap accountStatus = null;

        // Component States
        ////////////////////////////////////////////////////

        protected StringComponentState AccountStatusState = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public VoipDialerComponent()
            : base() 
        {
            this.AccountStatusState = new StringComponentState();
            this.AccountStatusState.OnProcessUpdate += new CCI.SimplSharp.Library.Components.EventArguments.StringEventHandler(AccountStatusState_OnProcessUpdate);
            this.States.Add(this.AccountStatusState);

            this.accountStatus = new VoipAccountStatusMap();
        }

        // Exposed
        ////////////////////////////////////////////////////

        public override void Configure(ushort CommandProcessorId, string ModuleName)
        {
            base.Configure(CommandProcessorId, ModuleName);

            this.VoipAccountStatusInfo = new BoseModuleModel();
            this.VoipAccountStatusInfo.ModuleName = ModuleName;
            this.VoipAccountStatusInfo.Index1 = "0";
            this.VoipAccountStatusInfo.Index2 = "0";

            this.CallStatusInfo = new BoseModuleModel();
            this.CallStatusInfo.ModuleName = ModuleName;
            this.CallStatusInfo.Index1 = "0";
            this.CallStatusInfo.Index2 = "1";

            this.CallerIdInfo = new BoseModuleModel();
            this.CallerIdInfo.ModuleName = ModuleName;
            this.CallerIdInfo.Index1 = "0";
            this.CallerIdInfo.Index2 = "2";

            this.CallActiveInfo = new BoseModuleModel();
            this.CallActiveInfo.ModuleName = ModuleName;
            this.CallActiveInfo.Index1 = "0";
            this.CallActiveInfo.Index2 = "6";

            this.AutoAnswerInfo = new BoseModuleModel();
            this.AutoAnswerInfo.ModuleName = ModuleName;
            this.AutoAnswerInfo.Index1 = "0";
            this.AutoAnswerInfo.Index2 = "7";

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

            this.TransferCallInfo = new BoseModuleModel();
            this.TransferCallInfo.ModuleName = ModuleName;
            this.TransferCallInfo.Index1 = "5";

        }

        public void TransferCall(String DialString)
        { 
            if (this.IsInitialized && String.IsNullOrEmpty(DialString) == false)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleActionMessage(this, this.TransferCallInfo, DialString));
        }


        // Helper Methods
        ////////////////////////////////////////////////////

        private void processAccountStatus(String status)
        {
            string pattern = @"[^\""]+";

            Match m = Regex.Match(status, pattern);

            if (m.Success)
                this.AccountStatusState.UpdateState(m.Value);         
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

            if (status.Length < 1)
            {
                this.callerId.Clear();
            }
            else if (status.Contains("<"))
            {
                string pattern = @"(?<name>[\S|\s]+)<(?<number>[\S|\s]+)>";

                Match m = Regex.Match(status, pattern);

                if (m.Success)
                {
                    this.callerId.Name = m.Groups["name"].Value.Trim();
                    this.callerId.Number = m.Groups["number"].Value.Trim();
                }
            }
            else
            {
                this.callerId.Name = status.Trim() == "???" ? String.Empty : status.Trim();
                this.callerId.Number = status.Trim() == "???" ? String.Empty : status.Trim();
            }

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
                            case "0": // Account Status Response
                                this.processAccountStatus(r.Response.Value);
                                break;
                            
                            case "1": // Call State Response

                                this.processCallStatus(r.Response.Value);
                                break;

                            case "2": // Caller ID Response
                                this.processCallerId(r.Response.Value);
                                break;

                            case "6": // Call Active State Response
                                this.processCallActiveState(r.Response.Value);
                                break;

                            case "7": // Auto Answer Response
                                this.processAutoAnswerState(r.Response.Value);
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
            if (this.OnVoipAccountStatusChange != null)
                this.OnVoipAccountStatusChange(this, new VoipAccountStatusEventArgs(this.accountStatus));

            base.Refresh();
        }

        public override void Reinitialize()
        {
            this.accountStatus = new VoipAccountStatusMap();            
            base.Reinitialize();
        }

        protected override void Subscribe()
        {
            if (this.Ready)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSubscribeMessage(this, this.VoipAccountStatusInfo));

            base.Subscribe();
        }

        protected override void Unsubscribe()
        {
            if (this.Ready)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleUnsubscribeMessage(this, this.VoipAccountStatusInfo));

            base.Unsubscribe();
        }

        // Component States Events
        ////////////////////////////////////////////////////

        void AccountStatusState_OnProcessUpdate(object sender, CCI.SimplSharp.Library.Components.EventArguments.StringEventArgs args)
        {
            this.accountStatus = VoipAccountStatusMap.Find(args.Payload);

            if (this.accountStatus is VoipAccountStatusMap && this.OnVoipAccountStatusChange != null)
                this.OnVoipAccountStatusChange(this, new VoipAccountStatusEventArgs(this.accountStatus));

            this.updateInitializationChange();
        }
    }
}