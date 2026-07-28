﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axl.Base.Models
{
    public class WmiQuery
    {
        public string Name { get; set; }
        public string Nmspace { get; set; } // Namespace (ej: root\cimv2)
        public string CLspace { get; set; } // Class (ej: Win32_OperatingSystem)
        public Dictionary<string, string> Queries { get; set; } // Key: Alias, Value: PropertyName
    }
}

