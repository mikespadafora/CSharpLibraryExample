using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.Common;
using BoseExSeriesLib.ProtocolSupport;
using BoseExSeriesLib.Interfaces;
using BoseExSeriesLib.EventArguments;
using CCI.SimplSharp.Library.Components.States;
using CCI.SimplSharp.Library.IO.Utilities;
using BoseExSeriesLib.ProtocolSupport.Models;
using CCI.SimplSharp.Library.Components.Registration;
using BoseExSeriesLib.Enums;
using System.Collections;

namespace BoseExSeriesLib.Components
{
    public abstract class ADialerComponent : IComponent<MessageBundle, IResponse>, IQuarantine, IRefresh, IDisposable
    {
        // Events
        ////////////////////////////////////////////////////

        public event InitializationEventHandler OnInitializationChange;
        public event ComponentQuarantinedEventHandler OnQuarantinedChange;
        public event CallActiveEventHandler OnCallActiveChange;
        public event CallStatusEventHandler OnCallStatusChange;
        public event CallerIdEventHandler OnCallerIdChange;
        public event KeypadTextEventHandler OnKeypadTextChange;
        public event AutoAnswerEventHandler OnAutoAnswerChange;

        // Constants
        ////////////////////////////////////////////////////

        private const UInt16 AUTO_ANSWER_MAX_RING_COUNT = 8;
        private const UInt16 AUTO_ANSWER_DEFAULT_RING_COUNT = 1;
        private const UInt16 AUTO_ANSWER_OFF_VALUE = 0;

        // Protocol
        ////////////////////////////////////////////////////

        protected IBoseModel CallStatusInfo = null;
        protected IBoseModel CallerIdInfo = null;
        protected IBoseModel AutoAnswerInfo = null;
        protected IBoseModel CallActiveInfo = null;

        protected IBoseModel DialKeyInfo = null;
        protected IBoseModel MakeCallInfo = null;
        protected IBoseModel EndCallInfo = null;
        protected IBoseModel AnswerCallInfo = null;

        // Add protected BoseModuleModel TransferCallInfo = null; to VoipComponent.


        // Members
        ////////////////////////////////////////////////////

        protected Boolean Ready = false;
        protected IProcessor<MessageBundle> listener = null;

        protected Boolean IsInitialized = false;

        protected Boolean IsQuarantined = false;

        protected Boolean subscribed = false;

        protected StringBuilder KeypadText = null;

        protected CallerIdInfo callerId = null;

        protected CallStatusMap callStatus = null;

        // Properties
        ////////////////////////////////////////////////////

        public UInt16 AutoAnswerRingCount { get; protected set; }
        public String ModuleName { get; private set; }

        protected Boolean _autoAnswer
        {
            get
            {
                if (this.AutoAnswerState.IsInitialized())
                    return (bool)this.AutoAnswerState.State;
                else
                    return false;
            }
        }

        protected Boolean _callActive
        {
            get
            {
                if (this.CallActiveState.IsInitialized())
                    return (bool)this.CallActiveState.State;
                else
                    return false;
            }
        }

        // Component States
        ////////////////////////////////////////////////////

        protected List<IComponentState> States = null;

        protected BooleanComponentState CallActiveState = null;
        protected StringComponentState CallStatusState = null;
        protected BooleanComponentState AutoAnswerState = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public ADialerComponent()
        {
            this.KeypadText = new StringBuilder();
            this.callerId = new CallerIdInfo();
            this.callStatus = new CallStatusMap();

            this.States = new List<IComponentState>();

            this.CallActiveState = new BooleanComponentState();
            this.CallStatusState = new StringComponentState();
            this.AutoAnswerState = new BooleanComponentState();

            this.CallActiveState.OnProcessUpdate += new CCI.SimplSharp.Library.Components.EventArguments.BooleanEventHandler(CallActiveState_OnProcessUpdate);
            this.CallStatusState.OnProcessUpdate += new CCI.SimplSharp.Library.Components.EventArguments.StringEventHandler(CallStatus_OnProcessUpdate);
            this.AutoAnswerState.OnProcessUpdate += new CCI.SimplSharp.Library.Components.EventArguments.BooleanEventHandler(AutoAnswerState_OnProcessUpdate);

            this.States.Add(this.CallActiveState);
            this.States.Add(this.CallStatusState);
            this.States.Add(this.AutoAnswerState);
        }

        // Exposed
        ////////////////////////////////////////////////////

        public virtual void Configure(UInt16 CommandProcessorId, String ModuleName)
        {
            this.CommandProcessorId = CommandProcessorId;
            this.ModuleName = ModuleName;

            this.Id = Registrar.GetNextComponentId(this);

            Registrar.Register(this);

            this.Ready = true;
        }

        public virtual void Configure(UInt16 CommandProcessorId, String ModuleName, UInt16 CountryCode)
        {
            this.Configure(CommandProcessorId, ModuleName);
        }

        public void AnswerCall()
        {
            if (this.IsInitialized)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleActionMessage(this, this.AnswerCallInfo));
        }

        public void EndCall()
        {
            if (this.IsInitialized)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleActionMessage(this, this.EndCallInfo));
        }

        public void DialDigit(String Digit)
        {
            this.KeypadText.Append(Digit);
            
            if (this.IsInitialized && this._callActive)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleActionMessage(this, this.DialKeyInfo, Digit));

            this.updateKeypadText();
        }

        public void DialKeypadText()
        {
            if (this.IsInitialized && this.KeypadText.Length > 0)
            {
                this.Dial(this.KeypadText.ToString());

                this.KeypadText.Length = 0;
            }
        }

        public void Dial(String DialString)
        {
            if (this.IsInitialized && String.IsNullOrEmpty(DialString) == false)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleActionMessage(this, this.MakeCallInfo, DialString));
        }

        public void ClearKeypadText()
        {
            this.KeypadText.Length = 0;

            this.updateKeypadText();
        }

        public void BackspaceKeypadText()
        {
            if (this.KeypadText.Length > 0)
            {
                this.KeypadText.Length--;
                this.updateKeypadText();
            }
        }

        public void SetAutoAnswerRingCount(UInt16 count)
        {
            if (count >= 0 && count <= AUTO_ANSWER_MAX_RING_COUNT)
            {
                this.AutoAnswerRingCount = count;

                if (this.IsInitialized && this._autoAnswer)
                    this.AutoAnswerOn();
            }
        }

        public void AutoAnswerOn()
        { 
            if (this.IsInitialized)
            {
                ushort value = this.AutoAnswerRingCount > AUTO_ANSWER_OFF_VALUE ? this.AutoAnswerRingCount : AUTO_ANSWER_DEFAULT_RING_COUNT;

                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.AutoAnswerInfo, value.ToString()));
            }
        }

        public void AutoAnswerOff()
        {
            if (this.IsInitialized)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.AutoAnswerInfo, AUTO_ANSWER_OFF_VALUE.ToString()));
        }

        public void AutoAnswerToggle()
        {
            if (this.IsInitialized)
            {
                if (this._autoAnswer)
                    this.AutoAnswerOff();
                else
                    this.AutoAnswerOn();
            }
        }

        public void UnRegister()
        {
            Registrar.UnRegister(this);

            ((IInitialize)this).Reinitialize();

            this.subscribed = false;

            this.updateInitializationChange();
        }

        // Helper Methods
        ////////////////////////////////////////////////////

        public abstract void Poll();

        protected virtual void Subscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSubscribeMessage(this, this.CallStatusInfo));
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSubscribeMessage(this, this.CallerIdInfo));
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSubscribeMessage(this, this.AutoAnswerInfo));
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSubscribeMessage(this, this.CallActiveInfo));
            }
        }

        protected virtual void Unsubscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleUnsubscribeMessage(this, this.CallStatusInfo));
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleUnsubscribeMessage(this, this.CallerIdInfo));
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleUnsubscribeMessage(this, this.AutoAnswerInfo));
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleUnsubscribeMessage(this, this.CallActiveInfo));
            }
        }

        private void processIntializationChange(Boolean state)
        {
            if (state)
                this.processQuarantineChange(false);

            if (this.OnInitializationChange != null)
                this.OnInitializationChange(this, new InitializationEventArgs(CrestronSimplPlusHelper.ToCrestronBool(state)));
        }

        protected void updateInitializationChange()
        {
            bool state = ((IComponent)this).IsInitialized();

            if (this.IsInitialized != state)
            {
                this.IsInitialized = state;
                this.processIntializationChange(state);
            }
        }

        protected void processQuarantineChange(bool state)
        {
            if (this.OnQuarantinedChange != null)
                this.OnQuarantinedChange(this, new ComponentQuarantinedEventArgs(CrestronSimplPlusHelper.ToCrestronBool(state)));

            this.IsQuarantined = state;
        }

        private void updateKeypadText()
        {
            if (this.OnKeypadTextChange != null)
                this.OnKeypadTextChange(this, new KeypadTextEventArgs(this.KeypadText.ToString()));
        }

        protected void ProcessCallerId()
        {
            if (this.callerId is CallerIdInfo && this.OnCallerIdChange != null)
                this.OnCallerIdChange(this, new CallerIdEventArgs(this.callerId));
        }

        // Component States Events
        ////////////////////////////////////////////////////


        void AutoAnswerState_OnProcessUpdate(object sender, CCI.SimplSharp.Library.Components.EventArguments.BooleanEventArgs args)
        {
            if (this.OnAutoAnswerChange != null)
                this.OnAutoAnswerChange(this, new AutoAnswerEventArgs(CrestronSimplPlusHelper.ToCrestronBool((bool)args.Payload)));

            this.updateInitializationChange();
        }

        void CallStatus_OnProcessUpdate(object sender, CCI.SimplSharp.Library.Components.EventArguments.StringEventArgs args)
        {
            if (String.IsNullOrEmpty(args.Payload) == false)
            {
                this.callStatus = CallStatusMap.Find(args.Payload);

                if (callStatus is CallStatusMap && this.OnCallStatusChange != null)
                    this.OnCallStatusChange(this, new CallStatusEventArgs(callStatus));

                this.updateInitializationChange();
            }
        }

        void CallActiveState_OnProcessUpdate(object sender, CCI.SimplSharp.Library.Components.EventArguments.BooleanEventArgs args)
        {
            if (this.OnCallActiveChange != null)
                this.OnCallActiveChange(this, new CallActiveEventArgs(CrestronSimplPlusHelper.ToCrestronBool(args.Payload)));

            this.updateInitializationChange();
        }

        // IQuarantine
        ////////////////////////////////////////////////////

        public Boolean Quarantined()
        {
            return this.IsQuarantined;
        }


        // IComponent<MessageBundle, MessageBundle>
        ////////////////////////////////////////////////////

        public abstract void ProcessSubscription(IResponse response);

        IProcessor<MessageBundle> IComponent<MessageBundle, IResponse>.Processor
        {
            get { return this.listener; }
        }

        // IResponseHandler<MessageBundle, MessageBundle>
        ////////////////////////////////////////////////////

        public abstract void ProcessResponse(IResponse response, MessageBundle request);

        // IComponent
        ////////////////////////////////////////////////////

        public UInt16 CommandProcessorId { get; private set; }

        public UInt16 Id { get; private set; } 

        void IComponent.GetInitialized()
        {
            if (((IInitialize)(this)).IsInitialized() == false && this.listener is IProcessor)
            {
                this.Unsubscribe();

                if (!this.subscribed)
                    this.Subscribe();
                else
                    this.Poll();
            }
        }

        void IComponent.UpdateMainProcess(IProcessor processor)
        {
            if (processor is IProcessor)
                this.listener = (IProcessor<MessageBundle>)processor;
        }

        // IInitialize
        ////////////////////////////////////////////////////

        bool IInitialize.IsInitialized()
        {
            foreach (var state in this.States)
            {
                if (state.IsInitialized() == false)
                    return false;
            }
            return true;
        }

        public virtual void Reinitialize()
        {
            try
            {
                this.subscribed = false;
                
                foreach (var state in this.States)
                    state.Reinitialize();

                this.ClearKeypadText();

                this.callerId = new CallerIdInfo();
                this.callStatus = new CallStatusMap();

                this.processQuarantineChange(false);

                ((IRefresh)this).Refresh();

                this.updateInitializationChange();
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("StateComponent.Reinitialize.Exception: {0}", ex.Message);
            }
        }

        // IRefresh
        ////////////////////////////////////////////////////

        public virtual void Refresh()
        { 
            if (this.OnCallActiveChange != null)
                this.OnCallActiveChange(this, new CallActiveEventArgs(CrestronSimplPlusHelper.ToCrestronBool(this._callActive)));

            if (this.OnAutoAnswerChange != null)
                this.OnAutoAnswerChange(this, new AutoAnswerEventArgs(CrestronSimplPlusHelper.ToCrestronBool(this._autoAnswer)));

            if (this.OnCallerIdChange != null)
                this.OnCallerIdChange(this, new CallerIdEventArgs(this.callerId));

            if (this.OnCallStatusChange != null)
                this.OnCallStatusChange(this, new CallStatusEventArgs(this.callStatus));
            
        }

        // IDisposable
        ////////////////////////////////////////////////////

        public virtual void Dispose()
        {
            try
            {
                Registrar.UnRegister(this);

                if (this.OnInitializationChange != null)
                    this.OnInitializationChange = null;

                if (this.OnQuarantinedChange != null)
                    this.OnQuarantinedChange = null;

                if (this.listener != null)
                    this.listener = null;

                if (this.States is IList)
                {
                    this.States.Clear();
                    this.States = null;
                }                
            }
            catch
            { 
            
            }
        }

    }
}