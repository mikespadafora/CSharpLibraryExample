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
    public class LevelComponent : IComponent<MessageBundle, IResponse>, IBoseModule, IQuarantine, IRefresh, IDisposable
    {
        // Events
        ////////////////////////////////////////////////////

        public event InitializationEventHandler OnInitializationChange;
        public event ComponentQuarantinedEventHandler OnQuarantinedChange;
        public event VolumeLevelEventHandler OnVolumeLevelChange;
        public event VolumePercentageEventHandler OnVolumePercentageChange;

        // Members
        ////////////////////////////////////////////////////

        protected Boolean Ready = false;
        protected IProcessor<MessageBundle> listener = null;

        protected Boolean IsInitialized = false;

        protected Boolean IsQuarantined = false;

        protected Boolean subscribed = false;

        private CTimer RampTimer = null;
        private RampTypes currentRampType = RampTypes.STOP;

        // Properties
        ////////////////////////////////////////////////////

        public LevelAttributes ModuleType { get; private set; }

        public UInt16 ModuleChannel { get; private set; }

        public UInt16 LevelStep  { get; protected set; }
        public Int16 UpperLimit { get; protected set; }
        public Int16 LowerLimit { get; protected set; }

        public IBoseModel protocol { get; private set; }

        // Contstants
        ////////////////////////////////////////////////////

        private const long RAMP_TIME = 500;

        // Component States
        ////////////////////////////////////////////////////
        
        protected List<IComponentState> States = null;

        protected AnalogComponentState VolumePercentage = null;
        protected SignedAnalogComponentState VolumeLevel = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public LevelComponent()
        {
            this.States = new List<IComponentState>();

            this.VolumePercentage = new AnalogComponentState();
            this.VolumeLevel = new SignedAnalogComponentState();

            this.VolumePercentage.OnProcessUpdate += new AnalogEventHandler(VolumePercentage_OnProcessUpdate);
            this.VolumeLevel.OnProcessUpdate += new SignedAnalogEventHandler(VolumeLevel_OnProcessUpdate);

            this.States.Add(this.VolumePercentage);
            this.States.Add(this.VolumeLevel);
        }

        // Exposed
        ////////////////////////////////////////////////////

        public void Configure(UInt16 CommandProcessorId, String ModuleName, UInt16 ModuleType, UInt16 ModuleChannel, Int16 UpperLimit, Int16 LowerLimit, UInt16 LevelStep)
        {
            this.CommandProcessorId = CommandProcessorId;
            this.ModuleType = (LevelAttributes)ModuleType;
            this.ModuleChannel = ModuleChannel;
            this.UpperLimit = UpperLimit;
            this.LowerLimit = LowerLimit;
            this.LevelStep = LevelStep;

            var model = LevelAttributeFactory.CreateLevelModel(this.ModuleType);

            if (model is IBoseModel)
                this.protocol = model.GetProtocolInfo(ModuleName, ModuleChannel);

            this.Id = Registrar.GetNextComponentId(this);
            Registrar.Register(this);

            this.Ready = true;
        }

        public void Raise()
        {
            if (this.IsInitialized)
                this.RampLevel(RampTypes.UP);
        }

        public void Lower()
        {
            if (this.IsInitialized)
                this.RampLevel(RampTypes.DOWN);
        }

        public void Stop()
        {
            if (this.IsInitialized)
                this.RampLevel(RampTypes.STOP);
        }

        public virtual void SetVolumeLevel(Int16 Level)
        {
            if (this.IsInitialized)
            {
                if (Level <= this.UpperLimit && Level >= this.LowerLimit)
                {
                    if (this.ModuleType != LevelAttributes.InputGain)
                        this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, Level.ToString()));
                    else
                    { 
                        Int16[] values = { 0, 14, 24, 32, 44, 54, 64 };

                        short nearest = values.OrderBy(x => Math.Abs((long)x - Level)).First();

                        this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, nearest.ToString()));
                    }
                }
            }
        }

        public virtual void SetVolumePercentage(UInt16 Percentage)
        {
            if (this.IsInitialized)
            {
                short level = ScaleUtil.ConvertFromPercentage(this, Percentage);

                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, level.ToString()));
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

        protected void updateInitializationChange()
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

        protected virtual void IncrementLevel()
        {
            short level = ScaleUtil.GetNextLevel(this, RampTypes.UP, (short)this.VolumeLevel.State);

            this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, level.ToString()), QueuePriorities.Cmd);
        }

        protected virtual void DecrementLevel()
        {
            short level = ScaleUtil.GetNextLevel(this, RampTypes.DOWN, (short)this.VolumeLevel.State);

            this.listener.Enqueue(this, ProtocolUtil.BuildModuleSetMessage(this, this.protocol, level.ToString()), QueuePriorities.Cmd);
        }

        protected virtual void Poll()
        {
            if (this.Ready)
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleGetMessage(this, this.protocol));
        }

        protected virtual void Subscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildModuleSubscribeMessage(this, this.protocol), QueuePriorities.Cmd);
            }
        }

        protected virtual void Unsubscribe()
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

        void VolumeLevel_OnProcessUpdate(object sender, SignedAnalogEventArgs args)
        {
            if (this.OnVolumeLevelChange != null)
                this.OnVolumeLevelChange(this, new VolumeLevelEventArgs(args.Payload));

            this.updateInitializationChange();
        }

        void VolumePercentage_OnProcessUpdate(object sender, AnalogEventArgs args)
        {
            if (this.OnVolumePercentageChange != null)
                this.OnVolumePercentageChange(this, new VolumePercentageEventArgs(args.Payload));

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

        public virtual void ProcessSubscription(IResponse response)
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

                        decimal dec = Decimal.Parse(r.Response.Value);
                        Int16 value = (Int16)Math.Round(dec);

                        if (value < this.LowerLimit)
                            value = this.LowerLimit;
                        else if (value > this.UpperLimit)
                            value = this.UpperLimit;

                        UInt16 percent = ScaleUtil.ConvertToPercentage(this, value);

                        if (this.VolumeLevel.State != value)
                            this.VolumeLevel.UpdateState(value.ToString());

                        if (this.VolumePercentage.State != percent)
                            this.VolumePercentage.UpdateState(percent.ToString());
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

        public UInt16 CommandProcessorId { get; protected set; }

        public UInt16 Id { get; protected set; } 

        public virtual void GetInitialized()
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
                CrestronConsole.PrintLine("LevelComponent.Reinitialize.Exception: {0}", ex.Message);
            }
        }

        // IRefresh
        ////////////////////////////////////////////////////

        public virtual void Refresh()
        {
            short level = this.VolumeLevel.State == null ? (short)0 : (short)this.VolumeLevel.State;
            ushort percent = this.VolumePercentage.State == null ? (ushort)0 : (ushort)this.VolumePercentage.State;   

            if (this.OnVolumeLevelChange != null)
                this.OnVolumeLevelChange(this, new VolumeLevelEventArgs(level));

            if (this.OnVolumePercentageChange != null)
                this.OnVolumePercentageChange(this, new VolumePercentageEventArgs(percent));
             
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

                if (this.OnVolumeLevelChange != null)
                    this.OnVolumeLevelChange = null;

                if (this.OnVolumePercentageChange != null)
                    this.OnVolumePercentageChange = null;

                if (this.listener != null)
                    this.listener = null;

                if (this.VolumeLevel != null)
                {
                    this.VolumeLevel.OnProcessUpdate -= VolumeLevel_OnProcessUpdate;
                    this.VolumeLevel = null;
                }

                if (this.VolumePercentage != null)
                {
                    this.VolumePercentage.OnProcessUpdate -= VolumePercentage_OnProcessUpdate;
                    this.VolumePercentage = null;
                }

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