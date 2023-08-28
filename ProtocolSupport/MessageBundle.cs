using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.IO.Common;
using BoseExSeriesLib.Enums;
using BoseExSeriesLib.ProtocolSupport.Models;
using System.Text.RegularExpressions;

namespace BoseExSeriesLib.ProtocolSupport
{
    public class MessageBundle
    {
        public Object sender { get; set; }
        public ByteBuffer message { get; set; }
        public IBoseModel protocol { get; set; }
        public RequestType requestType { get; set; }
        public String ValidateResponse { get; set; }

        public MessageBundle()
        {
            this.message = new ByteBuffer();
        }

        public MessageBundle(Object sender, RequestType requestType, IBoseModel protocol, ByteBuffer message)
        {
            this.sender = sender;
            this.message = message;
            this.protocol = protocol;
            this.requestType = requestType;
        }

        public Boolean IsMyResponse(String OriginalMessage)
        {
            if (OriginalMessage.StartsWith("\x15"))
                return true;

            var m = Regex.Match(OriginalMessage, this.ValidateResponse);

            return m.Success;
        }
    }
}