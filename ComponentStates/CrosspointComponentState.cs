using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Components.States;
using CCI.SimplSharp.Library.Components.EventArguments;

namespace BoseExSeriesLib.ComponentStates
{
    public class CrosspointComponentState : ComponentState<UInt32?>
    {
        public event GenericEventHandler<UInt32> OnProcessUpdate;

        public CrosspointComponentState()
            : base(default(UInt32?))
        {
        }

        public CrosspointComponentState(UInt32 State)
            : base(State)
        {
        }

        public CrosspointComponentState(UInt32 state, UInt32 initialState)
            : base(state, initialState)
        {
        }

        public override void UpdateState(String value)
        {
            UInt32 state = Convert.ToUInt32(value, 16);

            // Only update state if it hasn't been set or it's value will change
            if (this.State is UInt32? == false || state.Equals(this.State) == false)
            {
                this.State = state;

                // Signal the state has changed
                if (this.OnProcessUpdate != null)
                    this.OnProcessUpdate(this, new GenericEventArgs<UInt32>(state));
            }
        }
    }
}