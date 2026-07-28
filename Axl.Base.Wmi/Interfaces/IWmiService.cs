using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    public interface IWmiService
    {
        List<Dictionary<string, string>> Read(string deviceId, string ip, string user, string password, WmiQuery wma, out string error, int port = 0);
    }
}

