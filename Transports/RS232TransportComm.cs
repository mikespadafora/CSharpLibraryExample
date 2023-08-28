using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using BoseExSeriesLib.Interfaces;
using CCI.SimplSharp.Library.IO.Utilities;

namespace BoseExSeriesLib.Transports
{
    public class RS232TransportComm : IRS232Transport
    {
        // Members
        ////////////////////////////////////////////////////

        private IRS232Listener myRS232Listener = null;
        private ITransportListener myTransportListener = null;

        private CMutex _lock = null;

        private String buffer = null;

        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////

        public RS232TransportComm(IRS232Listener listener)
        {
            myRS232Listener = listener;
            this.buffer = String.Empty;
            this._lock = new CMutex();
        }

        // ITransport Members
        ////////////////////////////////////////////////////

        void ITransport.Configure(ITransportListener listener)
        {
            this.myTransportListener = listener;
        }

        void ITransport.Configure(ITransportListener listener, string Username, string Password)
        {
            //
        }

        void ITransport.SendMessage(byte[] Msg)
        {
            ((ITransport)this).SendMessage(Msg, Msg.Length);
        }

        void ITransport.SendMessage(byte[] Msg, int ByteCount)
        {
            try
            {
                var msg = StringUtil.toString(Msg, ByteCount);

                if (this.myRS232Listener is IRS232Listener)
                    this.myRS232Listener.ToDevice(msg);
            }
            catch (Exception)
            {
            }
        }

        // IRS232Transport Members
        ////////////////////////////////////////////////////

        void IRS232Transport.FromDevice(string msg)
        {
            try
            {
                //CrestronConsole.PrintLine("Pass 2");
                //CrestronConsole.PrintLine("Message: {0}", msg);

                this._lock.WaitForMutex();

                this.buffer += msg;

                if (this.buffer.Length > 0)
                {
                    var data = StringUtil.toByteArray(this.buffer);

                    if (this.myTransportListener is ITCPTransportListener)
                        this.myTransportListener.DataReceived(data, data.Length);

                    this.buffer = String.Empty;
                }
            }
            catch
            {
            }
            finally
            {
                this._lock.ReleaseMutex();
            }
        }

        // IDisposable
        ////////////////////////////////////////////////////

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}