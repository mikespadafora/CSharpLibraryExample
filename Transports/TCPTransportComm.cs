using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using BoseExSeriesLib.Interfaces;
using Crestron.SimplSharp.CrestronSockets;
using CCI.SimplSharp.Library.IO.Common;

namespace BoseExSeriesLib.Transports
{
    public class TCPTransportComm : ITCPTransport
    {
        // Properties
        ////////////////////////////////////////////////////

        private String IPAddress { get; set; }
        private UInt16 Port { get; set; }

        // Members
        ////////////////////////////////////////////////////

        private ITCPTransportListener listener = null;
        private TCPClient client = null;
        private ByteBuffer myBuffer = null;

        // Constants
        ////////////////////////////////////////////////////

        private const int BUFFER_SIZE = 1024;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public TCPTransportComm()
        {
            this.myBuffer = new ByteBuffer();
        }

        // ITransport Members
        ////////////////////////////////////////////////////

        void ITransport.Configure(ITransportListener listener, string Username, string Password)
        {
            //
        }

        void ITransport.Configure(ITransportListener listener)
        {
            if (listener is ITCPTransportListener)
                this.listener = (ITCPTransportListener)listener;
        }

        void ITransport.SendMessage(byte[] Msg)
        {
            ((ITransport)this).SendMessage(Msg, Msg.Length);
        }

        void ITransport.SendMessage(byte[] Msg, int ByteCount)
        {
            try
            {
                if (this.client is TCPClient)
                    this.client.SendData(Msg, ByteCount);
            }
            catch (Exception ex)
            {
                this.OnError(ex.Message);
            }
        }

        // ITCPTransportListener Helpers
        ////////////////////////////////////////////////////

        private void OnError(String msg)
        {
            if (this.listener is ITCPTransportListener)
                this.listener.Error(msg);
        }

        private void OnDataReceived(Byte[] Data, int ByteCount)
        {
            if (this.listener is ITCPTransportListener)
                this.listener.DataReceived(Data, ByteCount);
        }

        private void OnConnectionChange(Boolean State)
        {
            if (this.listener is ITCPTransportListener)
                this.listener.ConnectStatusChange(State);
        }


        // ITCPTransport Members
        ////////////////////////////////////////////////////

        void ITCPTransport.SetIPAddress(string IPAddress)
        {
            if (String.IsNullOrEmpty(IPAddress) == false)
                this.IPAddress = IPAddress;
        }

        void ITCPTransport.SetPortNumber(ushort Port)
        {
            if (Port > 0)
                this.Port = Port;
        }

        void ITCPTransport.Connect()
        {            
            try
            {
                if (this.client == null)
                {
                    this.client = new TCPClient(this.IPAddress, this.Port, BUFFER_SIZE);
                    this.client.SocketStatusChange += new TCPClientSocketStatusChangeEventHandler(client_SocketStatusChange);

                    this.client.ConnectToServerAsync(this.client_TCPClientConnectCallback);
                }
            }
            catch (Exception ex)
            {
                this.OnError(ex.Message);
            }
        }

        void ITCPTransport.Disconnect()
        {
            try
            {                
                if (this.client is TCPClient)
                {
                    SocketErrorCodes code = this.client.DisconnectFromServer();

                    ((IDisposable)this).Dispose();
                }
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Exception: {0}", ex.Message);
            }

            
        }

        bool ITCPTransport.IsConnected()
        {
            if (this.client is TCPClient)
                return this.client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED;

            return false;
        }

       
        // TCPClient Callbacks
        ////////////////////////////////////////////////////

        void client_TCPClientConnectCallback(TCPClient myTCPClient)
        {
            if (this.client is TCPClient && this.client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                this.client.ReceiveDataAsync(this.client_TCPClientReceiveCallback);

            else
                this.OnConnectionChange(false);
        }

        void client_SocketStatusChange(TCPClient myTCPClient, SocketStatus clientSocketStatus)
        {
            switch (clientSocketStatus)
            {
                case SocketStatus.SOCKET_STATUS_CONNECTED:
                    this.OnConnectionChange(true);
                    break;

                case SocketStatus.SOCKET_STATUS_DNS_LOOKUP:
                case SocketStatus.SOCKET_STATUS_DNS_RESOLVED:
                case SocketStatus.SOCKET_STATUS_WAITING:
                case SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY:
                case SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY:
                case SocketStatus.SOCKET_STATUS_CONNECT_FAILED:
                case SocketStatus.SOCKET_STATUS_DNS_FAILED:
                case SocketStatus.SOCKET_STATUS_LINK_LOST:
                case SocketStatus.SOCKET_STATUS_NO_CONNECT:
                case SocketStatus.SOCKET_STATUS_SOCKET_NOT_EXIST:
                    this.OnConnectionChange(false);
                    break;
            }
        }

        void client_TCPClientReceiveCallback(TCPClient client, int numberOfBytesReceived)
        {
            if (numberOfBytesReceived == 0)
                return;

            try
            {
                this.myBuffer.Append(client.IncomingDataBuffer, numberOfBytesReceived);

                if (this.myBuffer.Length() > 0)
                {
                    byte[] data = this.myBuffer.ToArray();

                    this.myBuffer.Clear();

                    this.OnDataReceived(data, data.Length);
                }
            }
            catch (Exception ex)
            {
                this.OnError(ex.Message);
            }
            finally
            {
                if (this.client is TCPClient)
                    this.client.ReceiveDataAsync(this.client_TCPClientReceiveCallback);
            }
        }

        // IDisposable
        ////////////////////////////////////////////////////

        void IDisposable.Dispose()
        {
            if (this.client is TCPClient)
            {
                this.client.Dispose();
                this.client = null;
            }
        }
    }
}