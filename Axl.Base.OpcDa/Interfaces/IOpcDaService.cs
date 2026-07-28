﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    public interface IOpcDaService
    {
        OPCRes Read(string deviceId, string svrName, string ip, Dictionary<string, string> parameters, out string error, int retry = 1);
    }
}

