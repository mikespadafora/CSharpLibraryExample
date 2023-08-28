using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.Common;
using BoseExSeriesLib.ProtocolSupport;
using BoseExSeriesLib.Interfaces;
using System.Collections;
using CCI.SimplSharp.Library.Components.Registration;
using BoseExSeriesLib.Enums;
using BoseExSeriesLib.ProtocolSupport.Models;
using CCI.SimplSharp.Library.Components.States;
using BoseExSeriesLib.EventArguments;
using CCI.SimplSharp.Library.IO.Utilities;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.Components
{
    public class RouterComponent : IComponent<MessageBundle, IResponse>, IBoseModule, IQuarantine, IRefresh, IDisposable
    {
        // Events
        ////////////////////////////////////////////////////

        public event InitializationEventHandler OnInitializationChange;
        public event ComponentQuarantinedEventHandler OnQuarantinedChange;
        public event InputRoutedEventHandler OnInputRoutedChange;

        // Members
        ////////////////////////////////////////////////////

        protected Boolean Ready = false;
        protected IProcessor<MessageBundle> listener = null;

        protected Boolean IsInitialized = false;

        protected Boolean IsQuarantined = false;

        private Boolean subscribed = false;

        // Properties
        ////////////////////////////////////////////////////

        public IBoseModel protocol { get; private set; }

        // Contstants
        ////////////////////////////////////////////////////
        private UInt16 MAX_INPUT = 32;

        // Component States
        ////////////////////////////////////////////////////
        protected AnalogComponentState InputRouted = null;
        protected List<IComponentState> States = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public RouterComponent()
        {
            this.InputRouted = new AnalogComponentState();
            this.InputRouted.OnProcessUpdate += new AnalogEventHandler(SourceSelection_OnProcessUpdate);

            this.States = new List<IComponentState>();
            this.States.Add(this.InputRouted);
        }

        // Exposed
        ////////////////////////////////////////////////////

        public void Configure(UInt16 CommandProcessorId, String ModuleName, UInt16 ModuleOutput)
        {
            this.CommandProcessorId = CommandProcessorId;

            this.Id = Registrar.GetNextComponentId(this);

            this.protocol = new BoseModuleModel();
            this.protocol.ModuleName = ModuleName;
            this.protocol.Index1 = ModuleOutput.ToString();

            Registrar.Register(this);

            this.Ready = true;
        }

        public void UnRegister()
        {
            Registrar.UnRegister(this);

            ((IInitialize)this).Reinitialize();

            this.subscribed = false;

            this.updateInitializationChange();
        }

        public void RouteInput(UInt16 Input)
        {
            if (this.IsInitialized)
            {
                if (Input >= 0 && Input <= MAX_INPUT)
                {
                    if (this.InputRouted.State != Input)
                    {
                        var msg = ProtocolUtil.BuildModuleSetMessage(this, this.protocol, Input.ToString());

                        this.listener.Enqueue(this, msg);
                    }
                }
            }
        }

        public void DerouteInput(UInt16 Input)
        {
            if (this.IsInitialized)
            {
                if (Input > 0 && Input <= MAX_INPUT)
                {
                    if (this.InputRouted.State == Input)
                    {
                        var msg = ProtocolUtil.BuildModuleSetMessage(this, this.protocol, UInt16.MinValue.ToString());
                         this.listener.Enqueue(this, msg);;
                    }
                }
            }
        }

        public void ToggleInput(UInt16 Input)
        {
            if (this.IsInitialized)
            {
                MessageBundle msg = null;

                if (Input > 0 && Input <= MAX_INPUT)
                {
                    if (this.InputRouted.State == Input)
                        msg = ProtocolUtil.BuildModuleSetMessage(this, this.protocol, UInt16.MinValue.ToString());

                    else
                        msg = ProtocolUtil.BuildModuleSetMessage(this, this.protocol, Input.ToString());

                    this.listener.Enqueue(this, msg);;
                }
            }
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

        void SourceSelection_OnProcessUpdate(object sender, CCI.SimplSharp.Library.Components.EventArguments.AnalogEventArgs args)
        {
            if (this.OnInputRoutedChange != null)
                this.OnInputRoutedChange(this, new InputRoutedEventArgs(args.Payload));

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

                        this.InputRouted.UpdateState(r.Response.Value);
                    }
                }
                else if (response is ErrorResponse)
                {
                    ErrorResponse r = (ErrorResponse)response;

                    if (r.Error.ErrorCode == ErrorResponses.INVALID_MODULE_NAME.ErrorCode || r.Error.ErrorCode == ErrorResponses.ILLEGAL_INDEX.ErrorCode)
                        this.processQuarantineChange(true);
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
                CrestronConsole.PrintLine("SourceSelectorComponent.Reinitialize.Exception: {0}", ex.Message);
            }
        }

        // IRefresh
        ////////////////////////////////////////////////////

        void IRefresh.Refresh()
        {
            ushort state = this.InputRouted.IsInitialized() ? (ushort)this.InputRouted.State : UInt16.MinValue;
            
            if (this.OnInputRoutedChange != null)
                this.OnInputRoutedChange(this, new InputRoutedEventArgs(state));
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

                if (this.OnInputRoutedChange != null)
                    this.OnInputRoutedChange = null;

                if (this.InputRouted != null)
                    this.InputRouted = null;

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