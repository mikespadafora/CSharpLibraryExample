using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace BoseExSeriesLib.Interfaces
{
    public interface IRS232Transport : ITransport
    {
        void FromDevice(String msg);
    }
}