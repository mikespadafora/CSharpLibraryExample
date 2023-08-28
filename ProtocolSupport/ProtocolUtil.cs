using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.IO.Common;
using BoseExSeriesLib.Enums;
using BoseExSeriesLib.ProtocolSupport.Models;

namespace BoseExSeriesLib.ProtocolSupport
{
    public class ProtocolUtil
    {
        public static MessageBundle BuildModuleSetMessage(Object sender, IBoseModel protocol, String value)
        {
            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Set;
            payload.sender = sender;
            payload.protocol = protocol;
            payload.ValidateResponse = "\x06";

            string msg = String.Empty;

            if (protocol.Index2 != null)
                msg = String.Format("SA\"{0}\">{1}>{2}={3}", protocol.ModuleName, protocol.Index1, protocol.Index2, value);
            else
                msg = String.Format("SA\"{0}\">{1}={2}", protocol.ModuleName, protocol.Index1, value);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildModuleGetMessage(Object sender, IBoseModel protocol)
        {
            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Get;
            payload.sender = sender;
            payload.protocol = protocol;
            payload.ValidateResponse = "GA";

            string msg = String.Empty;

            if (protocol.Index2 != null)
                msg = String.Format("GA\"{0}\">{1}>{2}", protocol.ModuleName, protocol.Index1, protocol.Index2);
            else
                msg = String.Format("GA\"{0}\">{1}", protocol.ModuleName, protocol.Index1);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildModuleSubscribeMessage(Object sender, IBoseModel protocol)
        {
            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Subscribe;
            payload.sender = sender;
            payload.protocol = protocol;
            payload.ValidateResponse = "SUB";

            string msg = String.Format("SUB\"{0}\"", ProtocolUtil.BuildModuleGetMessage(sender, protocol).message);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildModuleUnsubscribeMessage(Object sender, IBoseModel protocol)
        {
            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Unsubscribe;
            payload.sender = sender;
            payload.protocol = protocol;
            payload.ValidateResponse = "UNS";

            string msg = String.Format("UNS\"{0}\"", ProtocolUtil.BuildModuleGetMessage(sender, protocol).message);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildModuleActionMessage(Object sender, IBoseModel protocol, String value)
        {
            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Action;
            payload.sender = sender;
            payload.protocol = protocol;
            payload.ValidateResponse = "\x06";

            string msg = String.Empty;

            if (String.IsNullOrEmpty(value) == false)
                msg = String.Format("MA\"{0}\">{1}=\"{2}\"", protocol.ModuleName, protocol.Index1, value);
            else
                msg = String.Format("MA\"{0}\">{1}", protocol.ModuleName, protocol.Index1, value);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildModuleActionMessage(Object sender, IBoseModel protocol)
        {
            return ProtocolUtil.BuildModuleActionMessage(sender, protocol, null);
        }

        public static MessageBundle BuildParameterSetMessage(Object sender, String value)
        {
            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.ParameterSetRequest;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "S";

            string msg = String.Format("SS {0}", value);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildParameterSetSubscribeMessage(Object sender)
        {
            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Subscribe;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "SUB";

            string msg = String.Format("SUB\"GS\"");

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildHeartbeat(Object sender)
        {
            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Heartbeat;
            payload.sender = sender;
            payload.ValidateResponse = "IP";

            string msg = "IP";

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildGroupLevelSetMessage(Object sender, UInt16 GroupNumber, String Value)
        {
            string group = GroupNumber.ToString("X");

            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.GroupLevelSet;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "GG";

            string msg = String.Format("SG {0},{1}", group, Value);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildGroupMuteSetMessage(Object sender, UInt16 GroupNumber, GroupStateValues State)
        {
            string group = GroupNumber.ToString("X");

            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.GroupMuteSet;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "GN";

            string msg = String.Format("SN {0},{1}", group, State.Value);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildGroupLevelSubscribeMessage(Object sender, UInt16 GroupNumber)
        {
            string group = GroupNumber.ToString("X");

            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Subscribe;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "SUB";

            string msg = String.Format("SUB\"GG {0}\"", group);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildGroupMuteSubscribeMessage(Object sender, UInt16 GroupNumber)
        {
            string group = GroupNumber.ToString("X");

            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Subscribe;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "SUB";

            string msg = String.Format("SUB\"GN {0}\"", group);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildGroupLevelUnsubscribeMessage(Object sender, UInt16 GroupNumber)
        {
            string group = GroupNumber.ToString("X");

            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Unsubscribe;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "UNS";

            string msg = String.Format("UNS\"GG {0}\"", group);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildGroupMuteUnsubscribeMessage(Object sender, UInt16 GroupNumber)
        {
            string group = GroupNumber.ToString("X");

            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Unsubscribe;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "UNS";

            string msg = String.Format("UNS\"GN {0}\"", group);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildGroupLevelGetMessage(Object sender, UInt16 GroupNumber)
        {
            string group = GroupNumber.ToString("X");

            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Get;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "GG";

            string msg = String.Format("GG {0}", group);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }

        public static MessageBundle BuildGroupMuteGetMessage(Object sender, UInt16 GroupNumber)
        {
            string group = GroupNumber.ToString("X");

            MessageBundle payload = new MessageBundle();

            payload.requestType = RequestType.Get;
            payload.sender = sender;
            payload.protocol = null;
            payload.ValidateResponse = "GN";

            string msg = String.Format("GN {0}", group);

            payload.message.Append(Encoding.ASCII.GetBytes(msg));

            return payload;
        }
    }
}