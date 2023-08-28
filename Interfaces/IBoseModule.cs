using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using BoseExSeriesLib.ProtocolSupport.Models;

namespace BoseExSeriesLib.Interfaces
{
    public interface IBoseModule
    {
        IBoseModel protocol { get; }
    }
}