using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Interfaces
{
    public interface ITransport : IDisposable
    {
        void Configure(ITransportListener listener);
        void Configure(ITransportListener listener, String Username, String Password);
        void SendMessage(Byte[] Msg);
        void SendMessage(Byte[] Msg, int ByteCount);
    }
}