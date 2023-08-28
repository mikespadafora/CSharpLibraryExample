using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Enums
{
    public sealed class VoipAccountStatusMap
    {
        private static List<VoipAccountStatusMap> myList = new List<VoipAccountStatusMap>();

        public readonly static VoipAccountStatusMap INCOMING = new VoipAccountStatusMap(1, "NOT_CONFIGURED");
        public readonly static VoipAccountStatusMap DIALING = new VoipAccountStatusMap(2, "CONFIGURED");
        public readonly static VoipAccountStatusMap RINGBACK = new VoipAccountStatusMap(3, "P2P_REGISTERED");
        public readonly static VoipAccountStatusMap ACTIVE = new VoipAccountStatusMap(4, "PROXY_REGISTERING");
        public readonly static VoipAccountStatusMap HANGUP = new VoipAccountStatusMap(5, "PROXY_REGISTERED");
        public readonly static VoipAccountStatusMap HOLD_STATE_PEER = new VoipAccountStatusMap(6, "PROXY_TIMEOUT");
        public readonly static VoipAccountStatusMap UNKNOWN = new VoipAccountStatusMap(99, "UNKOWN");

        public String Value { get; private set; }
        public UInt16 Index { get; private set; }

        private VoipAccountStatusMap(UInt16 Index, String Value)
        {
            this.Value = Value;
            this.Index = Index;

            myList.Add(this);
        }

        public VoipAccountStatusMap()
        {
            this.Value = String.Empty;
            this.Index = UInt16.MinValue;
        }

        public static VoipAccountStatusMap Find(String Value)
        {
            var item = myList.Find(x => x.Value == Value);

            if (item is VoipAccountStatusMap)
                return item;
            else
                return VoipAccountStatusMap.UNKNOWN;
        }
    }
}