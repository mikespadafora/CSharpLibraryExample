using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using BoseExSeriesLib.EventArguments;
using CCI.SimplSharp.Library.Components.States;
using CCI.SimplSharp.Library.Components.Registration;
using BoseExSeriesLib.ProtocolSupport;
using BoseExSeriesLib.Enums;
using CCI.SimplSharp.Library.IO.Utilities;
using CCI.SimplSharp.Library.Components.Common;

namespace BoseExSeriesLib.Components
{
    public class GroupLevelMuteComponent : LevelComponent
    {
        public event StateChangeEventHandler OnStateChange;

        protected BooleanComponentState StateValue = null;

        public UInt16 GroupNumber { get; private set; }

        private Int16 adjustedUpperLimit { get; set; }
        private Int16 adjustedLowerLimit { get; set; }
        private UInt16 adjustedLevelStep { get; set; }
        private Int16 adjustedCurrentLevel { get; set; }


        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public GroupLevelMuteComponent()
            : base()
        {
            this.StateValue = new BooleanComponentState();
            this.StateValue.OnProcessUpdate += new CCI.SimplSharp.Library.Components.EventArguments.BooleanEventHandler(StateValue_OnProcessUpdate);
            this.States.Add(this.StateValue);
        }

        // Exposed
        ////////////////////////////////////////////////////

        public void Configure(UInt16 CommandProcessorId, UInt16 GroupNumber, Int16 UpperLimit, Int16 LowerLimit, UInt16 LevelStep)
        {
            this.CommandProcessorId = CommandProcessorId;
            this.GroupNumber = GroupNumber;
            this.UpperLimit = UpperLimit;
            this.LowerLimit = LowerLimit;
            this.LevelStep = LevelStep;

            this.adjustedUpperLimit = (short)(144 - ((12 - this.UpperLimit) * 2));
            this.adjustedLowerLimit = (short)((this.LowerLimit + 60) * 2);
            this.adjustedLevelStep = (ushort)(this.LevelStep * 2);

            this.Id = Registrar.GetNextComponentId(this);
            Registrar.Register(this);

            this.Ready = true;
        }

        protected override void IncrementLevel()
        {
            short level = ScaleUtil.GetNextLevel(this.adjustedCurrentLevel, RampTypes.UP, this.adjustedUpperLimit, this.adjustedLowerLimit, this.adjustedLevelStep);
            string value = level.ToString("X");

            if (level != this.adjustedCurrentLevel)
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupLevelSetMessage(this, this.GroupNumber, value));
        }

        protected override void DecrementLevel()
        {
            short level = ScaleUtil.GetNextLevel(this.adjustedCurrentLevel, RampTypes.DOWN, this.adjustedUpperLimit, this.adjustedLowerLimit, this.adjustedLevelStep);
            string value = level.ToString("X");

            if (level != this.adjustedCurrentLevel)
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupLevelSetMessage(this, this.GroupNumber, value));
        }

        public override void SetVolumeLevel(Int16 Level)
        {
            if (this.IsInitialized)
            {
                if (Level <= this.UpperLimit && Level >= this.LowerLimit)
                {
                    short level = (short)((Level + 60) * 2);
                    string value = level.ToString("X");

                    if (level != this.adjustedCurrentLevel)
                        this.listener.Enqueue(this, ProtocolUtil.BuildGroupLevelSetMessage(this, this.GroupNumber, value));
                }
            }
        }

        public override void SetVolumePercentage(UInt16 Percentage)
        {
            if (this.IsInitialized)
            {
                short level = (short)((ScaleUtil.ConvertFromPercentage(this, Percentage) + 60) * 2);
                string value = level.ToString("X");

                if (level != this.adjustedCurrentLevel)
                    this.listener.Enqueue(this, ProtocolUtil.BuildGroupLevelSetMessage(this, this.GroupNumber, value));
            }
        }

        public void StateOn()
        {
            if (this.IsInitialized)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupMuteSetMessage(this, this.GroupNumber, GroupStateValues.On));
            }
        }

        public void StateOff()
        {
            if (this.IsInitialized)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupMuteSetMessage(this, this.GroupNumber, GroupStateValues.Off));
            }
        }

        public void StateToggle()
        {
            if (this.IsInitialized)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupMuteSetMessage(this, this.GroupNumber, GroupStateValues.Toggle));
            }
        }

        protected override void Subscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupLevelSubscribeMessage(this, this.GroupNumber));
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupMuteSubscribeMessage(this, this.GroupNumber));
            }
        }

        protected override void Unsubscribe()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupLevelUnsubscribeMessage(this, this.GroupNumber));
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupMuteUnsubscribeMessage(this, this.GroupNumber));
            }
        }

        protected override void Poll()
        {
            if (this.Ready)
            {
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupLevelGetMessage(this, this.GroupNumber));
                this.listener.Enqueue(this, ProtocolUtil.BuildGroupMuteGetMessage(this, this.GroupNumber));
            }
        }

        public override void ProcessSubscription(IResponse response)
        {
            if (response is IResponse)
            {
                if (response is GroupLevelSubscriptionResponse)
                {
                    GroupLevelSubscriptionResponse r = (GroupLevelSubscriptionResponse)response;

                    if (r.GroupNumber == this.GroupNumber)
                    {

                        this.adjustedCurrentLevel = r.Value;

                        decimal dec = (r.Value / 2) - 60;
                        short value = (short)Math.Round(dec);

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
                else if (response is GroupMuteSubscriptionResponse)
                {

                    GroupMuteSubscriptionResponse r = (GroupMuteSubscriptionResponse)response;

                    if (r.GroupNumber == this.GroupNumber)
                    {

                        string state = r.Value == GroupStateValues.On.Value ? "1" : "0";

                        this.StateValue.UpdateState(state);
                    }
                }
            }
        }

        public override void Refresh()
        {
            base.Refresh();

            bool state = this.StateValue.IsInitialized() ? (bool)this.StateValue.State : false;

            if (this.OnStateChange != null)
                this.OnStateChange(this, new StateEventArgs(CrestronSimplPlusHelper.ToCrestronBool(state)));
        }

        public override void GetInitialized()
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

        // Component States Events
        ////////////////////////////////////////////////////

        void StateValue_OnProcessUpdate(object sender, CCI.SimplSharp.Library.Components.EventArguments.BooleanEventArgs args)
        {
            if (this.OnStateChange != null)
                this.OnStateChange(this, new StateEventArgs(CrestronSimplPlusHelper.ToCrestronBool(args.Payload)));

            this.updateInitializationChange();
        }
    }
}