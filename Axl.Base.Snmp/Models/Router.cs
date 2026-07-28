﻿using System;
using System.Collections.Generic;

namespace Axl.Base.Models
{
    public class Router
    {
        public string SystemName { get; set; }
        public List<NetworkInt> Interfaces { get; set; }

        public Router()
        {
            Interfaces = new List<NetworkInt>();
        }

        public void SetSystemName(string name)
        {
            SystemName = name;
        }

        public void AddInterfaces(List<Dictionary<string, string>> data)
        {
            foreach (var item in data)
            {
                var netIf = new NetworkInt
                {
                    Index = item.ContainsKey("index") ? item["index"] : "0",
                    Description = item.ContainsKey("descripcion") ? item["descripcion"] : "Unknown",
                    Type = item.ContainsKey("tipo") ? item["tipo"] : "Unknown",
                    AdminStatus = item.ContainsKey("adminStatus") ? item["adminStatus"] : "Unknown",
                    OperStatus = item.ContainsKey("operStatus") ? item["operStatus"] : "Unknown",
                    InOctets = long.TryParse(item.ContainsKey("inOctets") ? item["inOctets"] : "0", out var inO) ? inO : 0,
                    OutOctets = long.TryParse(item.ContainsKey("outOctets") ? item["outOctets"] : "0", out var outO) ? outO : 0
                };
                Interfaces.Add(netIf);
            }
        }
    }

    public class NetworkInt
    {
        // Interfaz basada en IF-MIB
        public string Index { get; set; }
        public string Description { get; set; } // ifDescr
        public string Type { get; set; } // ifType (Numérico)
        public string AdminStatus { get; set; } // ifAdminStatus: 1=up, 2=down, 3=testing
        public string OperStatus { get; set; } // ifOperStatus: 1=up, 2=down, ...
        
        public long InOctets { get; set; } // ifInOctets
        public long OutOctets { get; set; } // ifOutOctets
        
        public bool IsUp => OperStatus == "1";
    }
}

