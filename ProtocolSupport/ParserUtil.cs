using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using BoseExSeriesLib.Enums;
using CCI.SimplSharp.Library.IO.Common;
using CCI.SimplSharp.Library.Components.Common;
using BoseExSeriesLib.Interfaces;
using System.Text.RegularExpressions;
using BoseExSeriesLib.ProtocolSupport.Models;

namespace BoseExSeriesLib.ProtocolSupport
{
    public interface IResponse
    {
        String OriginalResponse { get; set; }
        ResponseTypes ResponseType { get; set; }
    }

    public interface IResponse<T> : IResponse
    {
        T Response { get; set; }
    }

    public abstract class AResponse<T> : IResponse<T>
    {
        public abstract IResponse Parse(ByteBuffer response);
        public string OriginalResponse { get; set; }

        #region IResponse<T> Members

        public T Response { get; set; }

        #endregion

        #region IResponse Members

        public ResponseTypes ResponseType { get; set; }

        #endregion
    }


    public class ErrorResponse : AResponse<ByteBuffer>
    {
        public ErrorResponses Error { get; private set; }
        
        public override IResponse Parse(ByteBuffer response)
        {
            this.OriginalResponse = response.ToString();
            this.ResponseType = ResponseTypes.ERROR;
            this.Response = response.SubByteBuffer(1, 2);
            this.Error = ErrorResponses.Find(this.Response.ToString());

            return this;
        }
    }

    public class HeartbeatResponse : AResponse<ByteBuffer>
    {
        public override IResponse Parse(ByteBuffer response)
        {
            this.ResponseType = ResponseTypes.HEARTBEAT;
            this.OriginalResponse = response.ToString();

            this.Response = response;

            return this;
        }
    }

    public class RequestResponse : AResponse<ByteBuffer>
    {
        public override IResponse Parse(ByteBuffer response)
        {
            this.ResponseType = ResponseTypes.ACK;
            this.OriginalResponse = response.ToString();
            this.Response = response;

            return this;
        }
    }

    public class ModuleSubscriptionResponse : AResponse<ModuleSubscriptionResponseModel>
    {
        public override IResponse Parse(ByteBuffer response)
        {
            this.ResponseType = ResponseTypes.SUBSCRIPTION;
            this.OriginalResponse = response.ToString();

            this.Response = new ModuleSubscriptionResponseModel();

            string pattern = @"GA\""(?<Name>[\w|\d|\s|\W]+)\"">(?<Index1>[\d|\w]+)>?(?<Index2>[^>|^\s]+)?=(?<Value>[\S|\s]+)";

            Match m = Regex.Match(response.ToString(), pattern);

            if (m.Success)
            {
                this.Response.ModuleName = m.Groups["Name"].Value;
                this.Response.Index1 = m.Groups["Index1"].Value;
                this.Response.Index2 = m.Groups["Index2"].Value;
                this.Response.Value = m.Groups["Value"].Value;
            }

            return this;
        }
    }

    public class ParameterSetResponse : AResponse<UInt16>
    {
        public override IResponse Parse(ByteBuffer response)
        {
            this.ResponseType = ResponseTypes.PARAMETER_SET;
            this.OriginalResponse = response.ToString();

            string pattern = @"S ([\w|\d]+)";

            Match m = Regex.Match(response.ToString(), pattern);

            if (m.Success)
                this.Response = Convert.ToUInt16(m.Groups[1].Value, 16);
            else
                this.Response = UInt16.MinValue;

            return this;            
        }
    }

    public class SubscriptionRequestResponse : AResponse<ByteBuffer>
    {
        public Boolean Subscribed = false;

        public override IResponse Parse(ByteBuffer response)
        {
            this.ResponseType = ResponseTypes.SUBSCRIBE;
            this.OriginalResponse = response.ToString();

            if (response.IndexOf("SUB") >= 0 && response.IndexOf(",yes") >= 0)
            {
                this.Subscribed = true;
                this.Response = response;
            }

            else if (response.IndexOf("UNS") >= 0 && response.IndexOf(",yes") >= 0)
            {
                this.Subscribed = false;
                this.Response = response;
            }

            return this;
        }
    }

    public class GroupLevelSubscriptionResponse : AResponse<ByteBuffer>
    {
        public UInt16 GroupNumber { get; private set; }
        public Int16 Value { get; private set; }

        public override IResponse Parse(ByteBuffer response)
        {
            this.ResponseType = ResponseTypes.GROUP_LEVEL_RESPONSE;
            this.OriginalResponse = response.ToString();
            this.Response = response;

            string pattern = @"GG (?<group>[\S|\s]+),(?<value>[\S|\s]+)";

            Match m = Regex.Match(response.ToString(), pattern);

            if (m.Success)
            {
                this.GroupNumber = Convert.ToUInt16(m.Groups["group"].Value, 16);
                this.Value = Convert.ToInt16(m.Groups["value"].Value, 16);
            }

            return this;
        }
    }

    public class GroupMuteSubscriptionResponse : AResponse<ByteBuffer>
    { 
        public UInt16 GroupNumber { get; private set; }
        public String Value { get; private set; }

        public override IResponse Parse(ByteBuffer response)
        {
            this.ResponseType = ResponseTypes.GROUP_MUTE_RESPONSE;
            this.OriginalResponse = response.ToString();
            this.Response = response;

            string pattern = @"GN (?<group>[\S|\s]+),(?<value>[\S|\s]+)";

            Match m = Regex.Match(response.ToString(), pattern);

            if (m.Success)
            {
                this.GroupNumber = Convert.ToUInt16(m.Groups["group"].Value, 16);
                this.Value = m.Groups["value"].Value;
            }

            return this;
        }
    }
    
    public class ParserUtil
    {
        public static IResponse ParseResponse(ByteBuffer response)
        {
            try
            {
                IResponse resp = null;

                // Check for errors
                if (response.StartsWith(0x15))
                {
                    ErrorResponse r = new ErrorResponse();

                    resp = r.Parse(response);
                }

                // Check for ACK
                else if (response.StartsWith(0x06))
                {
                    RequestResponse r = new RequestResponse();

                    resp = r.Parse(response);
                }

                // Check for Heartbeat
                else if (response.StartsWith("IP"))
                {
                    HeartbeatResponse r = new HeartbeatResponse();

                    resp = r.Parse(response);
                }

                // Check for Subscription Request Response
                else if (response.StartsWith("SUB") || response.StartsWith("UNS"))
                {
                    SubscriptionRequestResponse r = new SubscriptionRequestResponse();

                    resp = r.Parse(response);
                }

                // Check for Subscription Response
                else if (response.StartsWith("GA"))
                {
                    ModuleSubscriptionResponse r = new ModuleSubscriptionResponse();

                    resp = r.Parse(response);
                }

                // Check for Parameter Set/Get Response
                else if (response.StartsWith("S "))
                {
                    ParameterSetResponse r = new ParameterSetResponse();

                    resp = r.Parse(response);
                }

                else if (response.StartsWith("GG "))
                {
                    GroupLevelSubscriptionResponse r = new GroupLevelSubscriptionResponse();

                    resp = r.Parse(response);
                }

                else if (response.StartsWith("GN "))
                {
                    GroupMuteSubscriptionResponse r = new GroupMuteSubscriptionResponse();

                    resp = r.Parse(response);
                }

                return resp;
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("BoseExSeriesLib.ProtocolSupport.ParserUtil.ParseResponse.Exception: {0}", ex.Message);
                ErrorLog.Error("BoseExSeriesLib.ProtocolSupport.ParserUtil.ParseResponse.Exception", ex);
                return null;
            }
        }
    }
}