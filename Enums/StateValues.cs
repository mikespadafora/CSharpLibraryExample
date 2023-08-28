using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Enums
{
    public class StateValues
    {
        private static List<StateValues> items = new List<StateValues>();

        public String Value { get; private set; }

        private StateValues(String Value)
        {
            items.Add(this);
            this.Value = Value;
        }

        public static StateValues Find(String Value)
        {
            var response = items.Find(x => x.Value == Value);

            if ((response is StateValues) == false)
                return StateValues.Unknown;
            else
                return response;
        }

        public readonly static StateValues On = new StateValues("O");
        public readonly static StateValues Off = new StateValues("F");
        public readonly static StateValues Toggle = new StateValues("T");
        public readonly static StateValues Pulse = new StateValues("P");
        public readonly static StateValues Unknown = new StateValues(String.Empty);
    }
}