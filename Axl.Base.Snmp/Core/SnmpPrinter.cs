﻿using System;
using System.Collections.Generic;
using System.Linq;
using Axl.Base.Interfaces;
using Axl.Base.Models;

namespace Axl.Base.Core
{
    public class SnmpPrinter
    {
        private readonly ISnmpService _snmpService;

        public SnmpPrinter(ISnmpService snmpService)
        {
            _snmpService = snmpService ?? throw new ArgumentNullException(nameof(snmpService));
        }

        public Printer GetInfo(string deviceId, string ip, int port, int version,
            string comm = "public", string user = "", string password = "", string privacy = "",
            int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
        {
            var printer = new Printer();
            string error = "";

            // OID para prtMarkerSuppliesEntry (1.3.6.1.2.1.43.11.1.1) del Printer-MIB
            var suppliesWalk = _snmpService.Walk(deviceId, ip, port, version, "1.3.6.1.2.1.43.11.1.1",
                out error, comm, user, password, privacy, to, demo, authProto, privProto);

            var suppliesList = new List<Dictionary<string, string>>();
            if (string.IsNullOrEmpty(error) && suppliesWalk != null)
            {
                var suppliesDict = new Dictionary<string, Dictionary<string, string>>();

                // OID prefixes de Printer-MIB
                var descPrefix = "1.3.6.1.2.1.43.11.1.1.6";    // prtMarkerSuppliesDescription
                var maxPrefix = "1.3.6.1.2.1.43.11.1.1.8";     // prtMarkerSuppliesMaxCapacity
                var levelPrefix = "1.3.6.1.2.1.43.11.1.1.9";   // prtMarkerSuppliesLevel

                foreach (var kvp in suppliesWalk)
                {
                    var parts = kvp.Key.Split('.');
                    if (parts.Length < 2) continue;
                    
                    // El index usualmente tiene dos partes al final para las tablas de impresoras (ej. 1.1)
                    var index = parts[parts.Length - 2] + "." + parts[parts.Length - 1];

                    if (!suppliesDict.ContainsKey(index))
                        suppliesDict[index] = new Dictionary<string, string>();

                    if (kvp.Key.StartsWith(descPrefix)) suppliesDict[index]["nombre"] = kvp.Value;
                    else if (kvp.Key.StartsWith(maxPrefix)) suppliesDict[index]["capacidad"] = kvp.Value;
                    else if (kvp.Key.StartsWith(levelPrefix)) suppliesDict[index]["nivel"] = kvp.Value;
                }

                foreach (var kvp in suppliesDict)
                {
                    var data = kvp.Value;
                    if (data.ContainsKey("nombre") && data.ContainsKey("capacidad") && data.ContainsKey("nivel"))
                    {
                        suppliesList.Add(data);
                    }
                }
            }

            printer.AddSupplies(suppliesList);
            return printer;
        }
    }
}

