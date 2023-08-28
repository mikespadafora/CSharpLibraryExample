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
using BoseExSeriesLib.ComponentStates;

namespace BoseExSeriesLib.Components
{
    public class StdCrosspointComponent : IComponent<MessageBundle, IResponse>, IBoseModule, IQuarantine, IRefresh, IDisposable
    {
        // Events
        ////////////////////////////////////////////////////

        public event InitializationEventHandler OnInitializationChange;
        public event ComponentQuarantinedEventHandler OnQuarantinedChange;
        public event CrosspointRoutedEventHandler OnCrosspointRoutedChange;

        // Members
        ////////////////////////////////////////////////////

        protected Boolean Ready = false;
        protected IProcessor<MessageBundle> listener = null;

        protected Boolean IsInitialized = false;

        protected Boolean IsQuarantined = false;

        private Boolean subscribed = false;

        private UInt16 moduleInput = UInt16.MinValue;

        // Properties
        ////////////////////////////////////////////////////

        public IBoseModel controlProtocol { get; private set; }
        public IBoseModel stateProtocol { get; private set; }

        // Contstants
        ////////////////////////////////////////////////////
        //private UInt16 MAX_INPUTS = 32;
        //private UInt16 MAX_OUTPUTS = 32;

        // Component States
        ////////////////////////////////////////////////////
        protected CrosspointComponentState CrosspointRouted = null;
        protected List<IComponentState> States = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public StdCrosspointComponent()
        {
            this.CrosspointRouted = new CrosspointComponentState();
            this.CrosspointRouted.OnProcessUpdate += new GenericEventHandler<uint>(CrosspointRouted_OnProcessUpdate);

            this.States = new List<IComponentState>();
            this.States.Add(this.CrosspointRouted);
        }

        // Exposed
        ////////////////////////////////////////////////////

        public void Configure(UInt16 CommandProcessorId, String ModuleName, UInt16 ModuleInput)
        {
            this.CommandProcessorId = CommandProcessorId;

            this.Id = Registrar.GetNextComponentId(this);

            this.moduleInput = ModuleInput;

            this.controlProtocol = new BoseModuleModel();
            this.controlProtocol.ModuleName = ModuleName;
            this.controlProtocol.Index1 = "4";

            this.stateProtocol = new BoseModuleModel();
            this.stateProtocol.ModuleName = ModuleName;
            this.stateProtocol.Index1 = "3";
            this.stateProtocol.Index2 = ModuleInput.ToString();

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

        public void Route(UInt16 Output)
        {
            if (this.IsInitialized)
            {
                this.controlProtocol.Index2 = String.Format("({0},{1})", this.moduleInput, Output);

                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.controlProtocol, "O"), QueuePriorities.Cmd);
            }
        }

        public void Deroute(UInt16 Output)
        {
            if (this.IsInitialized)
            {
                this.controlProtocol.Index2 = String.Format("({0},{1})", this.moduleInput, Output);

                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.controlProtocol, "F"), QueuePriorities.Cmd);
            }
        }

        public void Toggle(UInt16 Output)
        {
            if (this.IsInitialized)
            {
                this.controlProtocol.Index2 = String.Format("({0},{1})", this.moduleInput, Output);

                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.controlProtocol, "T"), QueuePriorities.Cmd);
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
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.controlProtocol));
        }

        private void Subscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSubscribeMessage(this, this.stateProtocol), QueuePriorities.Cmd);
            }
        }

        private void Unsubscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleUnsubscribeMessage(this, this.stateProtocol), QueuePriorities.Cmd);
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

        void CrosspointRouted_OnProcessUpdate(object sender, GenericEventArgs<uint> args)
        {
            if (this.OnCrosspointRoutedChange != null)
                this.OnCrosspointRoutedChange(this, new CrosspointRoutedEventArgs(args.Payload));

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

                    if (r.Response.ModuleName == this.stateProtocol.ModuleName && r.Response.Index1 == this.stateProtocol.Index1)
                    {
                        if (this.stateProtocol.Index2 != null)
                        {
                            if (this.stateProtocol.Index2 != r.Response.Index2)
                                return;
                        }

                        this.CrosspointRouted.UpdateState(r.Response.Value);
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
                CrestronConsole.PrintLine("StdCrosspointComponent.Reinitialize.Exception: {0}", ex.Message);
            }
        }

        // IRefresh
        ////////////////////////////////////////////////////

        void IRefresh.Refresh()
        {
            UInt32 state = this.CrosspointRouted.IsInitialized() ? (UInt32)this.CrosspointRouted.State : UInt32.MinValue;

            if (this.OnCrosspointRoutedChange != null)
                this.OnCrosspointRoutedChange(this, new CrosspointRoutedEventArgs(state));
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

                if (this.OnCrosspointRoutedChange != null)
                    this.OnCrosspointRoutedChange = null;

                if (this.CrosspointRouted != null)
                    this.CrosspointRouted = null;

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

        #region IBoseModule Members

        public IBoseModel protocol
        {
            get { return this.controlProtocol; }
        }

        #endregion
    }
}