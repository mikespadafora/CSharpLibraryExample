using System;
using System.Text;
using Crestron.SimplSharp;
using CCI.SimplSharp.Library.Comm.Common;
using CCI.SimplSharp.Library.IO.Common;
using CCI.SimplSharp.Library.Components.Common;
using CCI.SimplSharp.Library.Comm.Priority;
using BoseExSeriesLib.Interfaces;
using BoseExSeriesLib.Transports;
using System.Collections.Generic;
using CCI.SimplSharp.Library.Components.Registration;
using CCI.SimplSharp.Library.IO.Utilities;
using System.Collections;
using BoseExSeriesLib.EventArguments;
using CCI.SimplSharp.Library.Components.EventArguments;
using BoseExSeriesLib.ProtocolSupport;
using BoseExSeriesLib.Enums;
using BoseExSeriesLib.ProtocolSupport.Models;

namespace BoseExSeriesLib
{
    public class CommandProcessor : ICommUtil<MessageBundle>, IProcessor<MessageBundle>, ITCPTransportListener, IRS232Listener, IDisposable
    {
        // Events
        ////////////////////////////////////////////////////

        public event InitializationEventHandler OnInitializationChange;
        public event CommunicatingEventHandler OnCommunicationChange;
        public event DebugEventHandler OnDebugMessage;
        public event RegisteredComponentCountEventHandler OnRegisteredComponentCountChange;
        public event QuarantinedComponentCountEventHandler OnQuarantinedComponentCountChange;
        public event RS232TransmitEventHandler OnRS232Transmit;
        public event EventHandler OnReadyState;

        // Properties
        ////////////////////////////////////////////////////

        public Boolean Ready { get; private set; }
        public Boolean IsInitialized { get; private set; }
        public Boolean IsCommunicating { get; private set; }

        public UInt16 Id { get; private set; }

        protected Boolean DisconnectRequested = false;

        // Members
        ////////////////////////////////////////////////////

        private PriorityCommUtil<MessageBundle> commUtil = null;

        private IProcessor<MessageBundle> processor = null;

        private List<IComponent> myComponents = null;
        private List<IComponent> quarantinedComponents = null;

        private UInt16 quarantinedCount = UInt16.MinValue;

        private List<ITransport> myTransports = null;

        private ITransport myActiveTransport = null;

        private ByteBuffer myBuffer = null;

        private CCriticalSection myLock = null;

        private MessageBundle lastMessage = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public CommandProcessor()
        {
            this.IsCommunicating = false;
            this.IsInitialized = false;

            this.quarantinedComponents = new List<IComponent>();

            this.processor = this;

            this.myBuffer = new ByteBuffer();
            this.myLock = new CCriticalSection();

            this.myTransports = new List<ITransport>();
            this.myTransports.Add(new TCPTransportComm());
            this.myTransports.Add(new RS232TransportComm((IRS232Listener)this));
        }

        // Exposed
        ////////////////////////////////////////////////////

        public void Test()
        {
            ((ICommUtil)this).GetInitialized();
        }

        public void ToDevice(String message)
        {
            ByteBuffer buffer = new ByteBuffer();

            buffer.Append(Encoding.ASCII.GetBytes(message));

            MessageBundle payload = new MessageBundle();

            payload.message = buffer;
            payload.sender = this;

            ((ICommUtil<MessageBundle>)this).SendMessage(payload);
        }

        public void Configure(UInt16 Type, UInt16 CommandProcessorId, String IPAddress, UInt16 Port)
        {
            this.Id = CommandProcessorId;
            
            if (Type < this.myTransports.Count)
                this.myActiveTransport = this.myTransports[Type];

            if (this.myActiveTransport is ITCPTransport)
            {
                ((ITCPTransport)this.myActiveTransport).SetIPAddress(IPAddress);
                ((ITCPTransport)this.myActiveTransport).SetPortNumber(Port);
            }

            if (this.myActiveTransport is ITransport)
                ((ITransport)myActiveTransport).Configure(this);

            this.commUtil = new PriorityCommUtil<MessageBundle>(this);
            this.commUtil.DequeuePacerTime = 50;


            this.commUtil.AutoInitialize = true;

            Registrar.Register(this);
        }

        public void Connect()
        {
            if (this.Ready)
            {
                this.DisconnectRequested = false;

                if (this.myActiveTransport is ITCPTransport)
                {
                    var transport = (ITCPTransport)this.myActiveTransport;

                    if (transport.IsConnected() == false)
                        transport.Connect();
                }
                else if (this.myActiveTransport is ITransport)
                    this.commUtil.Start();
            }
        }

        public void Disconnect()
        {
            try
            {
                this.DisconnectRequested = true;

                if (this.myActiveTransport is ITCPTransport)
                    ((ITCPTransport)this.myActiveTransport).Disconnect();

                if (this.commUtil != null)
                    this.commUtil.Stop();

                this.updateInitializationChange();
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("BoseExSeriesLib.CommandProcessor.Disconnect.Exception", ex);
            }
        }

        public void Initialize(UInt16 State)
        {
            if (this.commUtil != null)
            {
                this.commUtil.AutoInitialize = CrestronSimplPlusHelper.FromCrestronBool(State);

                if (this.commUtil.AutoInitialize && this.IsCommunicating && this.IsInitialized == false)
                    ((ICommUtil)this).GetInitialized();
            }
        }

        public void RS232Response(String msg)
        {
            //CrestronConsole.PrintLine("pass 1");

            if (this.myActiveTransport is IRS232Transport)
            {
                //CrestronConsole.PrintLine("pass 1a");
                ((IRS232Transport)this.myActiveTransport).FromDevice(msg);
            }
        }

        // Helpers
        ////////////////////////////////////////////////////

        protected void processInitializationChange(Boolean state)
        {
            if (this.OnInitializationChange != null)
                this.OnInitializationChange(this, new InitializationEventArgs(CrestronSimplPlusHelper.ToCrestronBool(state)));
        }

        protected void updateInitializationChange()
        {
            bool state = ((ICommUtil)this).IsInitialized();

            if (this.IsInitialized != state)
            {
                this.IsInitialized = state;
                this.processInitializationChange(state);

                if (state && myComponents is IList)
                {
                    foreach (var c in myComponents)
                    {
                        if (c is IRefresh)
                            ((IRefresh)c).Refresh();
                    }
                }
            }
        }

        protected void processCommunicationChange(bool State)
        {
            this.IsCommunicating = State;

            if (this.OnCommunicationChange != null)
                this.OnCommunicationChange(this, new CommunicatingEventArgs(CrestronSimplPlusHelper.ToCrestronBool(State)));
        }

        protected void processDebugMessage(String format, params object[] args)
        {
            this.processDebugMessage(String.Format(format, args));
        }

        protected void processDebugMessage(String msg)
        {
            String m = String.Format("BoseExSeriesLib{0}[ {1} ]\n", this.Id, msg);

            if (this.OnDebugMessage != null)
                this.OnDebugMessage(this, new DebugEventArgs(m));
        }

        protected void reinitialize()
        {
            if (this.myComponents is IList)
            {
                foreach (var c in this.myComponents)
                    c.Reinitialize();
            }
        }

        protected void updateQuarantinedCount()
        {
            if (this.quarantinedComponents is IList)
            {
                this.quarantinedCount = (ushort)this.quarantinedComponents.Count;

                if (this.OnQuarantinedComponentCountChange != null)
                    this.OnQuarantinedComponentCountChange(this, new QuarantinedComponentCountEventArgs(this.quarantinedCount));
            }
        }

        // ITCPTranportListener
        ////////////////////////////////////////////////////

        void ITCPTransportListener.ConnectStatusChange(bool State)
        {
            if (this.myBuffer is ByteBuffer)
                this.myBuffer.Clear();

            this.processDebugMessage("Connection Status Changed: {0}", State);

            if (State)
            {
                this.commUtil.StopReconnectionTimer();
                this.commUtil.Start();
            }
            else
            {
                this.reinitialize();
                this.processCommunicationChange(false);
                this.updateInitializationChange();

                if (!this.DisconnectRequested)
                    this.commUtil.StartReconnectionTimer();
            }
        }

        void ITCPTransportListener.Error(string ErrorMessage)
        {
            this.processDebugMessage("Error: {0}", ErrorMessage);
        }

        // ITranportListener
        ////////////////////////////////////////////////////

        void ITransportListener.DataReceived(byte[] Data, int ByteCount)
        {
            try
            {                
                this.myLock.Enter();

                this.myBuffer.Append(Data, ByteCount);

                while (this.myBuffer.Length() > 0)
                {
                    ByteBuffer r = new ByteBuffer();

                    switch (this.myBuffer.ByteAt(0))
                    {
                        case 0x06:
                            r = this.myBuffer.SubByteBuffer(0, 1);
                            this.myBuffer.Delete(0, 1);
                            break;

                        case 0x0d:
                            this.myBuffer.Delete(0, 1);
                            break;

                        case 0x15:
                            r = this.myBuffer.SubByteBuffer(0, 3);
                            this.myBuffer.Delete(0, 3);
                            break;

                        default:

                            int location = -1;

                            if ((location = this.myBuffer.IndexOf(";\r")) >= 0)
                            {
                                r = this.myBuffer.SubByteBuffer(0, location);
                                this.myBuffer.Delete(0, r.Length() + 2);
                            }
                            else if (this.myBuffer.IndexOf(";") >= 0 || this.myBuffer.IndexOf("\r") >= 0)
                            {
                                int loc1 = this.myBuffer.IndexOf(";");
                                int loc2 = this.myBuffer.IndexOf("\r");

                                if (loc1 >= 0 && loc2 >= 0)
                                    location = loc1 < loc2 ? loc1 : loc2;
                                else if (loc1 >= 0)
                                    location = loc1;
                                else
                                    location = loc2;

                                r = this.myBuffer.SubByteBuffer(0, location);
                                this.myBuffer.Delete(0, r.Length() + 1);
                            }

                            break;
                    }

                    IResponse response = ParserUtil.ParseResponse(r);

                    if (response is IResponse)
                    {
                        this.processDebugMessage("Received: {0} | {1}", response.OriginalResponse, response.GetType().FullName);


                        if (response is ErrorResponse)
                        {                            
                            if (this.lastMessage is MessageBundle && this.lastMessage.sender is IComponent<MessageBundle, IResponse>)
                                ((IComponent<MessageBundle, IResponse>)this.lastMessage.sender).ProcessResponse(response, this.lastMessage);

                            this.processDebugMessage("Error: {0}", ((ErrorResponse)response).Error.ErrorMessage);
                        }

                        else if (response is ModuleSubscriptionResponse)
                        {
                            if (((IResponse<ModuleSubscriptionResponseModel>)response).Response is ModuleSubscriptionResponseModel && this.myComponents is IList)
                            {
                                foreach (var c in this.myComponents)
                                {
                                    if (c is IComponent<MessageBundle, IResponse>)
                                    {
                                        ((IComponent<MessageBundle, IResponse>)c).ProcessSubscription(response);
                                    }
                                }
                            }
                        }

                        else if (response is GroupLevelSubscriptionResponse || response is GroupMuteSubscriptionResponse)
                        {
                            if (this.myComponents is IList)
                            {
                                foreach (var c in this.myComponents)
                                {
                                    if (c is IComponent<MessageBundle, IResponse>)
                                    {
                                        ((IComponent<MessageBundle, IResponse>)c).ProcessSubscription(response);
                                    }
                                }
                            }
                        }

                        else if (response is SubscriptionRequestResponse)
                        {
                            if (this.lastMessage is MessageBundle && this.lastMessage.sender is IComponent<MessageBundle, IResponse>)
                                ((IComponent<MessageBundle, IResponse>)this.lastMessage.sender).ProcessResponse(response, this.lastMessage);
                        }

                        else if (response is ParameterSetResponse)
                        {
                            if (this.myComponents is IList)
                            {
                                foreach (var c in this.myComponents)
                                {
                                    if (c is IComponent<MessageBundle, IResponse>)
                                        ((IComponent<MessageBundle, IResponse>)c).ProcessSubscription(response);
                                }
                            }
                        }

                        else if (response is HeartbeatResponse)
                        {
                            this.processCommunicationChange(true);

                            this.commUtil.ReceivedHeartbeat();
                            this.processDebugMessage("Heartbeat Received!");
                        }

                        if (this.lastMessage.IsMyResponse(response.OriginalResponse))
                        {
                            this.commUtil.GoodResponse();
                            this.processDebugMessage("Good Response!: {0}", response.OriginalResponse);
                        }

                        this.updateInitializationChange();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("BoseExSeriesLib.CommandProcessor.DataReceived.Exception", ex);
            }
            finally
            {
                this.myLock.Leave();
            }
        }

        // IRS232Listener
        ////////////////////////////////////////////////////

        void IRS232Listener.ToDevice(string msg)
        {
            if (this.OnRS232Transmit != null)
                this.OnRS232Transmit(this, new RS232TransmitEventArgs(msg));
        }

        // ICommUtil<ByteBuffer>
        ////////////////////////////////////////////////////

        void ICommUtil<MessageBundle>.SendMessage(MessageBundle payload)
        {
            try
            {
                if (payload is MessageBundle && this.myActiveTransport is ITransport)
                {
                    if (payload.message is ByteBuffer)
                    {
                       
                        payload.message.Append(0x0D);

                        this.processDebugMessage("SendMessage: {0}", payload.message.ToString());

                        this.myActiveTransport.SendMessage(payload.message.ToArray(), payload.message.Length());

                        this.lastMessage = payload;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("BoseExSeriesLib.CommandProcessor.SendMessage.Exception", ex);
            }
        }

        // ICommUtil
        ////////////////////////////////////////////////////

        void  ICommUtil.FailedResponse()
        {
            this.processDebugMessage("FailedResponse!!!");
        }

        int  ICommUtil.GetHeartbeatTime()
        {
            return 60000;
        }

        int ICommUtil.GetResponseTime()
        {
            return 5000;
        }

        void  ICommUtil.GetInitialized()
        {
            this.processDebugMessage("GetInitialized!!!");

            this.commUtil.Enqueue(ProtocolUtil.BuildParameterSetSubscribeMessage(this));

            if (this.myComponents is IList)
            {
                foreach (var c in this.myComponents)
                {
                    if (c.IsInitialized() == false)
                    {
                        c.GetInitialized();
                    }
                }
            }
        }

        bool  ICommUtil.IsInitialized()
        {
            if (this.IsCommunicating)
            {
                if (this.myComponents is IList)
                {
                    foreach (var c in this.myComponents)
                    {
                        if (c.IsInitialized() == false)
                        {
                            if (c is IQuarantine)
                            {
                                if (((IQuarantine)c).Quarantined() == false)
                                    return false;
                                else
                                {
                                    if (this.quarantinedComponents is IList && this.quarantinedComponents.Contains(c) == false)
                                    {
                                        this.quarantinedComponents.Add(c);
                                        this.updateQuarantinedCount();
                                    }
                                }
                            }
                            else
                                return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        void  ICommUtil.Reconnect()
        {
            this.processDebugMessage("Reconnect!!!");

            this.Disconnect();
            this.Connect();
        }

        void  ICommUtil.SendHeartbeat()
        {
            if (this.commUtil.QueueSize() == 0)
            {
                this.processDebugMessage("Sending Heartbeat...");
                ((IProcessor<MessageBundle>)this).Enqueue(this, ProtocolUtil.BuildHeartbeat(this), QueuePriorities.Query);
            }
        }

        void  ICommUtil.SendTrace(string msg)
        {
            this.processDebugMessage("ComponentDebug: {0}", msg);
        }

        void  ICommUtil.Strikeout()
        {
            this.processDebugMessage("Strikeout!!!");
        }

        // IProcessor<ByteBuffer>
        ////////////////////////////////////////////////////

        void IProcessor<MessageBundle>.Enqueue(object sender, MessageBundle request, QueuePriorities priority)
        {
            this.commUtil.Enqueue(request, (UInt16)priority);
        }

        void IProcessor<MessageBundle>.Enqueue(object sender, MessageBundle request)
        {
            this.commUtil.Enqueue(request);
        }

        // IProcessor
        ////////////////////////////////////////////////////

        void  IProcessor.DebugMessage(DebugTypes Type, string format, params object[] args)
        {
            this.processDebugMessage(String.Format(format, args));
        }

        void  IProcessor.DebugMessage(DebugTypes Type, string msg)
        {
            this.processDebugMessage(msg);
        }

        void  IProcessor.RegistrationFinished()
        {
            this.myComponents = Registrar.GetMyComponents(this);

            if (this.myComponents is IList)
            {
                this.processDebugMessage("Registration Finished: {0}", this.myComponents.Count);

                if (this.OnRegisteredComponentCountChange != null)
                    this.OnRegisteredComponentCountChange(this, new RegisteredComponentCountEventArgs((ushort)this.myComponents.Count));

                foreach (var c in this.myComponents)
                    c.UpdateMainProcess(this);

                this.Ready = true;

                if (this.OnReadyState != null)
                    this.OnReadyState(this, new EventArgs());
            }
        }

        // IDisposable
        ////////////////////////////////////////////////////

        void  IDisposable.Dispose()
        {
            try
            {
                if (this.OnInitializationChange != null)
                    this.OnInitializationChange = null;

                if (this.OnCommunicationChange != null)
                    this.OnCommunicationChange = null;

                if (this.OnDebugMessage != null)
                    this.OnDebugMessage = null;

                if (this.OnRegisteredComponentCountChange != null)
                    this.OnRegisteredComponentCountChange = null;

                if (this.OnQuarantinedComponentCountChange != null)
                    this.OnQuarantinedComponentCountChange = null;

                if (this.OnRS232Transmit != null)
                    this.OnRS232Transmit = null;

                if (this.OnReadyState != null)
                    this.OnReadyState = null;

                if (this.commUtil != null)
                {
                    this.commUtil.Stop();
                    this.commUtil.Dispose();
                    this.commUtil = null;
                }

                if (this.processor != null)
                    this.processor = null;

                if (this.myComponents is IList)
                {
                    foreach (var c in this.myComponents)
                        ((IDisposable)c).Dispose();

                    this.myComponents.Clear();
                    this.myComponents = null;
                }

                if (this.quarantinedComponents is IList)
                {
                    this.quarantinedComponents.Clear();
                    this.quarantinedComponents = null;
                }

                if (this.myTransports is IList)
                {
                    this.myTransports.Clear();
                    this.myTransports = null;
                }

                if (this.myActiveTransport != null)
                {
                    this.myActiveTransport.Dispose();
                    this.myActiveTransport = null;
                }

                if (this.myBuffer is ByteBuffer)
                {
                    this.myBuffer.Clear();
                    this.myBuffer = null;
                }

                if (this.myLock is CCriticalSection)
                {
                    this.myLock.Leave();
                    this.myLock.Dispose();
                    this.myLock = null;
                }
            }
            catch (Exception ex)
            { 
                ErrorLog.Exception("BoseExSeriesLib.CommandProcessor.Dispose.Exception", ex);
            }
        }

    }
}
