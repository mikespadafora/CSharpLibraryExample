using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.Common;
using CCI.SimplSharp.Library.IO.Common;
using BoseExSeriesLib.ProtocolSupport;
using BoseExSeriesLib.Enums;
using CCI.SimplSharp.Library.Components.States;
using CCI.SimplSharp.Library.Components.EventArguments;
using CCI.SimplSharp.Library.Components.Registration;
using BoseExSeriesLib.ProtocolSupport.Models;
using BoseExSeriesLib.EventArguments;
using BoseExSeriesLib.Interfaces;
using CCI.SimplSharp.Library.IO.Utilities;
using System.Text.RegularExpressions;
using System.Collections;

namespace BoseExSeriesLib.Components
{
    public class StateComponent : IComponent<MessageBundle, IResponse>, IQuarantine, IBoseModule, IRefresh, IDisposable
    {
        // Events
        ////////////////////////////////////////////////////

        public event InitializationEventHandler OnInitializationChange;
        public event ComponentQuarantinedEventHandler OnQuarantinedChange;
        public event StateChangeEventHandler OnStateChange;

        // Members
        ////////////////////////////////////////////////////

        protected Boolean Ready = false;
        protected IProcessor<MessageBundle> listener = null;

        protected Boolean IsInitialized = false;

        protected Boolean IsQuarantined = false;

        private Boolean subscribed = false;

        // Properties
        ////////////////////////////////////////////////////

        public StateAttributes ModuleType { get; private set; }

        public UInt16 ModuleChannel { get; private set; }

        public IBoseModel protocol { get; private set; }

        // Component States
        ////////////////////////////////////////////////////
        
        protected List<IComponentState> States = null;

        protected BooleanComponentState StateValue = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public StateComponent()
        {
            this.States = new List<IComponentState>();

            this.StateValue = new BooleanComponentState();

            this.StateValue.OnProcessUpdate += new BooleanEventHandler(StateValue_OnProcessUpdate);

            this.States.Add(this.StateValue);
        }

        // Exposed
        ////////////////////////////////////////////////////

        public void Configure(UInt16 CommandProcessorId, String ModuleName, UInt16 ModuleType, UInt16 ModuleChannel)
        {
            this.CommandProcessorId = CommandProcessorId;
            this.ModuleType = (StateAttributes)ModuleType;
            this.ModuleChannel = ModuleChannel;

            var model = StateAttributeFactory.CreateStateModel(this.ModuleType);

            if (model is IBoseModel)
                this.protocol = model.GetProtocolInfo(ModuleName, ModuleChannel);

            this.Id = Registrar.GetNextComponentId(this);
            Registrar.Register(this);

            this.Ready = true;
        }

        public void StateOn()
        {
            if (this.IsInitialized)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, StateValues.On.Value));
            }
        }

        public void StateOff()
        {
            if (this.IsInitialized)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, StateValues.Off.Value));
            }
        }

        public void StateToggle()
        {
            if (this.IsInitialized)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, StateValues.Toggle.Value));
            }
        }

        public void StatePulse()
        {
            if (this.IsInitialized)
            {
                if (this.ModuleType == StateAttributes.LogicInputState || this.ModuleType == StateAttributes.LogicOutputState)
                    this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, StateValues.Pulse.Value));
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

        private void processIntializationChange(Boolean state)
        {
            if (state)
                this.processQuarantineChange(false);

            if (this.OnInitializationChange != null)
                this.OnInitializationChange(this, new InitializationEventArgs(CrestronSimplPlusHelper.ToCrestronBool(state)));
        }

        private void updateInitializationChange()
        {
            bool state = ((IComponent)this).IsInitialized();

            if (this.IsInitialized != state)
            {
                this.IsInitialized = state;
                this.processIntializationChange(state);
            }
        }

        private void Poll()
        {
            if (this.Ready)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
        }

        private void Subscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSubscribeMessage(this, this.protocol), QueuePriorities.Cmd);
            }
        }

        private void Unsubscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleUnsubscribeMessage(this, this.protocol), QueuePriorities.Cmd);
            }
        }

        private void processQuarantineChange(bool state)
        {
            if (this.OnQuarantinedChange != null)
                this.OnQuarantinedChange(this, new ComponentQuarantinedEventArgs(CrestronSimplPlusHelper.ToCrestronBool(state)));

            this.IsQuarantined = state;
        }

        // Component States Events
        ////////////////////////////////////////////////////

        void StateValue_OnProcessUpdate(object sender, BooleanEventArgs args)
        {
            if (this.OnStateChange != null)
                this.OnStateChange(this, new StateEventArgs(CrestronSimplPlusHelper.ToCrestronBool(args.Payload)));

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

        void IComponent<MessageBundle, IResponse>.ProcessSubscription(IResponse response)
        {
            if (response is IResponse)
            {
                if (response is ModuleSubscriptionResponse)
                {
                    ModuleSubscriptionResponse r = (ModuleSubscriptionResponse)response;
                    if (r.Response.ModuleName == this.protocol.ModuleName && r.Response.Index1 == this.protocol.Index1)
                    {
                        if (this.protocol.Index2 != null)
                        {
                            if (this.protocol.Index2 != r.Response.Index2)
                                return;
                        }

                        string state = r.Response.Value == StateValues.On.Value ? "1" : "0";

                        if (this.StateValue != null)
                        {
                            this.StateValue.UpdateState(state);
                        }
                    }
                }
            }
        }

        IProcessor<MessageBundle> IComponent<MessageBundle, IResponse>.Processor
        {
            get { return this.listener; }
        }

        // IResponseHandler<MessageBundle, MessageBundle>
        ////////////////////////////////////////////////////

        void IResponseHandler<MessageBundle, IResponse>.ProcessResponse(IResponse response, MessageBundle request)
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

        void IInitialize.Reinitialize()
        {
            try
            {
                this.subscribed = false;
                
                foreach (var state in this.States)
                    state.Reinitialize();

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

        void IRefresh.Refresh()
        {
            bool state = this.StateValue.State == null ? false : (bool)this.StateValue.State;

            if (this.OnStateChange != null)
                this.OnStateChange(this, new StateEventArgs(CrestronSimplPlusHelper.ToCrestronBool(state)));
        }

        // IDisposable
        ////////////////////////////////////////////////////

        void IDisposable.Dispose()
        {
            try
            {
                Registrar.UnRegister(this);

                if (this.OnInitializationChange != null)
                    this.OnInitializationChange = null;

                if (this.OnQuarantinedChange != null)
                    this.OnQuarantinedChange = null;

                if (this.OnStateChange != null)
                    this.OnStateChange = null;

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