using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Interfaces
{
    public interface ITCPTransport : ITransport
    {
        void SetIPAddress(String IPAddress);
        void SetPortNumber(UInt16 Port);
        void Connect();
        void Disconnect();
        bool IsConnected();
    }
}