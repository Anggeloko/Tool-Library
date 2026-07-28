﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    public interface IOpc : IDisposable
    {
        Task ConnectAsync();
        Task<Result<Dictionary<string, string>>> ReadNodesAsync(List<string> nodeIds);
        Task<Result<Dictionary<string, string>>> BrowseNodesAsync(string rootNodeId);
    }
}

