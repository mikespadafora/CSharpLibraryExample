using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Interfaces
{
    public interface ITCPTransportListener : ITransportListener
    {
        void ConnectStatusChange(bool State);
        void Error(String ErrorMessage);
    }
}