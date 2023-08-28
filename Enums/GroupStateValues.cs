using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Enums
{
    public class GroupStateValues
    {
        private static List<GroupStateValues> items = new List<GroupStateValues>();

        public String Value { get; private set; }

        private GroupStateValues(String Value)
        {
            items.Add(this);
            this.Value = Value;
        }

        public static GroupStateValues Find(String Value)
        {
            var response = items.Find(x => x.Value == Value);

            if ((response is GroupStateValues) == false)
                return GroupStateValues.Unknown;
            else
                return response;
        }

        public readonly static GroupStateValues On = new GroupStateValues("M");
        public readonly static GroupStateValues Off = new GroupStateValues("U");
        public readonly static GroupStateValues Toggle = new GroupStateValues("T");
        public readonly static GroupStateValues Unknown = new GroupStateValues(String.Empty);
    }
}