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
    public class GenericComponent : IComponent<MessageBundle, IResponse>, IBoseModule, IQuarantine, IRefresh, IDisposable
    {
        // Events
        ////////////////////////////////////////////////////

        public event InitializationEventHandler OnInitializationChange;
        public event ComponentQuarantinedEventHandler OnQuarantinedChange;

        public event VolumeLevelEventHandler OnAnalogValueChange;
        public event VolumePercentageEventHandler OnAnalogPercentageChange;

        public event StateChangeEventHandler OnDigitalStateChange;

        public event SerialEventHandler OnSerialStateChange;

        // Members
        ////////////////////////////////////////////////////

        protected Boolean Ready = false;
        protected IProcessor<MessageBundle> listener = null;

        protected Boolean IsInitialized = false;

        protected Boolean IsQuarantined = false;

        private Boolean subscribed = false;

        private CTimer RampTimer = null;
        private RampTypes currentRampType = RampTypes.STOP;

        // Properties
        ////////////////////////////////////////////////////

        public SignalTypes SignalType { get; private set; }

        public UInt16 LevelStep  { get; private set; }
        public Int16  UpperLimit { get; private set; }
        public Int16  LowerLimit { get; private set; }

        public Boolean EnableSubscription { get; private set; }

        public UInt16 ScalingOffset { get; private set; }

        public IBoseModel protocol { get; private set; }

        private Int16 _analogValue
        {
            get
            {
                if (this.AnalogValueState is SignedAnalogComponentState)
                {
                    if (this.AnalogValueState.IsInitialized())
                        return (short)this.AnalogValueState.State;
                }
                return 0;
            }
        }

        private UInt16 _analogPercentage
        {
            get
            {
                if (this.AnalogPercentageState is AnalogComponentState)
                {
                    if (this.AnalogPercentageState.IsInitialized())
                        return (ushort)this.AnalogPercentageState.State;
                }
                return UInt16.MinValue;
            }
        }

        private Boolean _digitalState
        {
            get
            {
                if (this.DigitalState is BooleanComponentState)
                {
                    if (this.DigitalState.IsInitialized())
                        return (bool)this.DigitalState.State;
                }
                return false;
            }
        }

        private String _serialState
        {
            get
            {
                if (this.SerialState is StringComponentState)
                {
                    if (this.SerialState.IsInitialized())
                        return this.SerialState.State;
                }
                return String.Empty;
            }
        }

        // Contstants
        ////////////////////////////////////////////////////

        private const long RAMP_TIME = 500;

        // Component States
        ////////////////////////////////////////////////////
        
        protected List<IComponentState> States = null;

        protected AnalogComponentState AnalogPercentageState = null;
        protected SignedAnalogComponentState AnalogValueState = null;

        protected BooleanComponentState DigitalState = null;

        protected StringComponentState SerialState = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public GenericComponent()
        {
            this.States = new List<IComponentState>();
        }

        // Exposed
        ////////////////////////////////////////////////////

        public void Configure(UInt16 CommandProcessorId, String ModuleName, UInt16 SignalType, UInt16 EnableSubscription, String Index1, String Index2, Int16 UpperLimit, Int16 LowerLimit, UInt16 LevelStep, UInt16 ScalingOffset)
        {
            this.CommandProcessorId = CommandProcessorId;

            this.SignalType = (SignalTypes)SignalType;
            this.EnableSubscription = CrestronSimplPlusHelper.FromCrestronBool(EnableSubscription);

            this.UpperLimit = UpperLimit;
            this.LowerLimit = LowerLimit;
            this.LevelStep = LevelStep;

            this.protocol = new BoseModuleModel();
            this.protocol.ModuleName = ModuleName;
            this.protocol.Index1 = Index1;
            this.protocol.Index2 = String.IsNullOrEmpty(Index2) == false ? Index2 : null;

            this.ScalingOffset = ScalingOffset;

            switch (this.SignalType)
            { 
                case SignalTypes.Digital:

                    this.DigitalState = new BooleanComponentState();
                    this.DigitalState.OnProcessUpdate += new BooleanEventHandler(DigitalState_OnProcessUpdate);
                    this.States.Add(this.DigitalState);
                    break;

                case SignalTypes.Analog:

                    this.AnalogValueState = new SignedAnalogComponentState();
                    this.AnalogValueState.OnProcessUpdate += new SignedAnalogEventHandler(AnalogValueState_OnProcessUpdate);

                    this.AnalogPercentageState = new AnalogComponentState();
                    this.AnalogPercentageState.OnProcessUpdate += new AnalogEventHandler(AnalogPercentageState_OnProcessUpdate);

                    this.States.Add(this.AnalogValueState);
                    this.States.Add(this.AnalogPercentageState);
                    break;

                case SignalTypes.Serial:

                    this.SerialState = new StringComponentState();
                    this.SerialState.OnProcessUpdate += new StringEventHandler(SerialState_OnProcessUpdate);
                    this.States.Add(this.SerialState);
                    break;
            }

            this.Id = Registrar.GetNextComponentId(this);
            Registrar.Register(this);

            this.Ready = true;
        }

        // Digital Opertations
        /////////////////////////////////////////////////

        public void DigitalOn()
        {
            if (this.IsInitialized && this.SignalType == SignalTypes.Digital)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, StateValues.On.Value));

                if (!this.EnableSubscription)
                    this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
            }
        }

        public void DigitalOff()
        {
            if (this.IsInitialized && this.SignalType == SignalTypes.Digital)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, StateValues.Off.Value));

                if (!this.EnableSubscription)
                    this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
            }
        }

        public void DigitalToggle()
        {
            if (this.IsInitialized && this.SignalType == SignalTypes.Digital)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, StateValues.Toggle.Value));

                if (!this.EnableSubscription)
                    this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
            }
        }


        // Analog Opertations
        /////////////////////////////////////////////////

        public void Raise()
        {
            if (this.IsInitialized && this.SignalType == SignalTypes.Analog)
                this.RampLevel(RampTypes.UP);
        }

        public void Lower()
        {
            if (this.IsInitialized && this.SignalType == SignalTypes.Analog)
                this.RampLevel(RampTypes.DOWN);
        }

        public void Stop()
        {
            if (this.IsInitialized && this.SignalType == SignalTypes.Analog)
                this.RampLevel(RampTypes.STOP);
        }

        public void SetAnalogValue(Int16 Value)
        {
            if (this.IsInitialized && this.SignalType == SignalTypes.Analog)
            {
                if (Value <= this.UpperLimit && Value >= this.LowerLimit)
                {
                    double val = (double)Value / (double)this.ScalingOffset;

                    this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, val.ToString()));

                    if (!this.EnableSubscription)
                        this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
                }
            }
        }

        public void SetAnalogPercentage(UInt16 Percentage)
        {
            if (this.IsInitialized && this.SignalType == SignalTypes.Analog)
            {
                short level = ScaleUtil.ConvertFromPercentage(Percentage, this.UpperLimit, this.LowerLimit);

                double val = (double)level / (double)this.ScalingOffset;

                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, val.ToString()));

                if (!this.EnableSubscription)
                    this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
            }
        }

        // Serial Opertations
        /////////////////////////////////////////////////

        public void SetSerialValue(String Value)
        {
            if (this.IsInitialized && this.SignalType == SignalTypes.Serial)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, Value));

                if (!this.EnableSubscription)
                    this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
            }
        }

        // Registration
        /////////////////////////////////////////////////

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

        private void RampLevel(RampTypes RampType)
        {
            this.currentRampType = RampType;

            switch (this.currentRampType)
            { 
                case RampTypes.STOP:
                    this.StopRampTimer();
                    break;

                case RampTypes.UP:
                    this.ResetRampTimer();
                    this.IncrementLevel();
                    break;

                case RampTypes.DOWN:
                    this.ResetRampTimer();
                    this.DecrementLevel();
                    break;
            }
        }

        private void IncrementLevel()
        {
            short level = ScaleUtil.GetNextLevel((short)this.AnalogValueState.State, RampTypes.UP, this.UpperLimit, this.LowerLimit, this.LevelStep);

            double val = (double)level / (double)this.ScalingOffset;

            this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, val.ToString()), QueuePriorities.Cmd);

            if (!this.EnableSubscription)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
        }

        private void DecrementLevel()
        {
            short level = ScaleUtil.GetNextLevel((short)this.AnalogValueState.State, RampTypes.DOWN, this.UpperLimit, this.LowerLimit, this.LevelStep);

            double val = (double)level / (double)this.ScalingOffset;

            this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, val.ToString()), QueuePriorities.Cmd);

            if (!this.EnableSubscription)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
        }

        public void Poll()
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

        void SerialState_OnProcessUpdate(object sender, StringEventArgs args)
        {
            if (this.OnSerialStateChange != null)
                this.OnSerialStateChange(this, new SerialEventArgs(args.Payload));

            this.updateInitializationChange();
        }

        void AnalogPercentageState_OnProcessUpdate(object sender, AnalogEventArgs args)
        {
            if (this.OnAnalogPercentageChange != null)
                this.OnAnalogPercentageChange(this, new VolumePercentageEventArgs(args.Payload));

            this.updateInitializationChange();
        }

        void AnalogValueState_OnProcessUpdate(object sender, SignedAnalogEventArgs args)
        {
            if (this.OnAnalogValueChange != null)
                this.OnAnalogValueChange(this, new VolumeLevelEventArgs(args.Payload));

            this.updateInitializationChange();
        }

        void DigitalState_OnProcessUpdate(object sender, BooleanEventArgs args)
        {
            if (this.OnDigitalStateChange != null)
                this.OnDigitalStateChange(this, new StateEventArgs(CrestronSimplPlusHelper.ToCrestronBool(args.Payload)));

            this.updateInitializationChange();
        }

        // Ramp Timer
        ////////////////////////////////////////////////////

        private void StartRampTimer()
        {
            if (this.RampTimer == null)
                this.RampTimer = new CTimer(OnElapsedRampTimer, RAMP_TIME);
        }

        private void StopRampTimer()
        {
            if (this.RampTimer is CTimer)
            {
                this.RampTimer.Stop();
                this.RampTimer.Dispose();
                this.RampTimer = null;
            }
        }

        private void ResetRampTimer()
        {
            this.StopRampTimer();
            this.StartRampTimer();
        }

        private void OnElapsedRampTimer(object sender)
        {
            this.RampLevel(this.currentRampType);
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

                        switch (this.SignalType)
                        { 
                            case SignalTypes.Digital:

                                string state = r.Response.Value == StateValues.On.Value ? "1" : "0";

                                if (this.DigitalState != null)
                                    this.DigitalState.UpdateState(state);

                                break;

                            case SignalTypes.Analog:

                                decimal dec = Decimal.Parse(r.Response.Value);
                                Int16 value = (Int16)(dec * this.ScalingOffset);

                                if (value < this.LowerLimit)
                                    value = this.LowerLimit;
                                else if (value > this.UpperLimit)
                                    value = this.UpperLimit;

                                UInt16 percent = ScaleUtil.ConvertToPercentage(value, this.UpperLimit, this.LowerLimit);

                                if (this.AnalogValueState.State != value)
                                    this.AnalogValueState.UpdateState(value.ToString());

                                if (this.AnalogPercentageState.State != percent)
                                    this.AnalogPercentageState.UpdateState(percent.ToString());

                                break;

                            case SignalTypes.Serial:

                                if (this.SerialState != null)
                                    this.SerialState.UpdateState(r.Response.Value);
                                break;
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
                if (this.EnableSubscription)
                    this.Unsubscribe();
                
                if (this.EnableSubscription && !this.subscribed)
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
                CrestronConsole.PrintLine("Generic.Reinitialize.Exception: {0}", ex.Message);
            }
        }

        // IRefresh
        ////////////////////////////////////////////////////

        void IRefresh.Refresh()
        {
            if (this.OnAnalogValueChange != null)
                this.OnAnalogValueChange(this, new VolumeLevelEventArgs(this._analogValue));

            if (this.OnAnalogPercentageChange != null)
                this.OnAnalogPercentageChange(this, new VolumePercentageEventArgs(this._analogPercentage));

            if (this.OnDigitalStateChange != null)
                this.OnDigitalStateChange(this, new StateEventArgs(CrestronSimplPlusHelper.ToCrestronBool(this._digitalState)));

            if (this.OnSerialStateChange != null)
                this.OnSerialStateChange(this, new SerialEventArgs(this._serialState));
        }

        // IDisposable
        ////////////////////////////////////////////////////

        void IDisposable.Dispose()
        {
            try
            {
                Registrar.UnRegister(this);

                this.StopRampTimer();

                if (this.OnInitializationChange != null)
                    this.OnInitializationChange = null;

                if (this.OnQuarantinedChange != null)
                    this.OnQuarantinedChange = null;

                if (this.OnAnalogValueChange != null)
                    this.OnAnalogValueChange = null;

                if (this.OnAnalogPercentageChange != null)
                    this.OnAnalogPercentageChange = null;

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