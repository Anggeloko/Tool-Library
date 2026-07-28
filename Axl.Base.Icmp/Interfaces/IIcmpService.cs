using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    public interface IIcmpService
    {
        PingElement Read(string deviceId, string ip, out string error, int timeout = 300, int attempts = 10);
        string GetIp(string deviceId, List<string> ips, out string error);
    }
}

