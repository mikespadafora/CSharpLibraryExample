using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Enums
{
    public sealed class CallStatusMap
    {
        private static List<CallStatusMap> myList = new List<CallStatusMap>();

        public readonly static CallStatusMap INCOMING = new CallStatusMap(1, "INCOMING");
        public readonly static CallStatusMap DIALING = new CallStatusMap(2, "DIALING");
        public readonly static CallStatusMap RINGBACK = new CallStatusMap(3, "RINGBACK");
        public readonly static CallStatusMap ACTIVE = new CallStatusMap(4, "ACTIVE");
        public readonly static CallStatusMap HANGUP = new CallStatusMap(5, "HANGUP");
        public readonly static CallStatusMap HOLD_STATE_PEER = new CallStatusMap(6, "HOLD_STATE_PEER");
        public readonly static CallStatusMap ERROR = new CallStatusMap(7, "ERROR");
        public readonly static CallStatusMap PROTOCOL = new CallStatusMap(8, "PROTOCOL");
        public readonly static CallStatusMap PEER_REJECTED = new CallStatusMap(9, "PEER_REJECTED");
        public readonly static CallStatusMap UNKNOWN = new CallStatusMap(99, "UNKOWN");

        public String Value { get;  set; }
        public UInt16 Index { get;  set; }

        private CallStatusMap(UInt16 Index, String Value)
        {
            this.Value = Value;
            this.Index = Index;

            myList.Add(this);
        }

        public CallStatusMap()
        {
            this.Value = String.Empty;
            this.Index = UInt16.MinValue;
        }

        public static CallStatusMap Find(String Value)
        {
            var item = myList.Find(x => x.Value == Value);

            if (item is CallStatusMap)
                return item;
            else
                return CallStatusMap.UNKNOWN;
        }
    }
}