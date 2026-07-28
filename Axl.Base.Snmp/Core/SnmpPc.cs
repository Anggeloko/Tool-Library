﻿using System;
using System.Collections.Generic;
using System.Linq;
using Axl.Base.Interfaces;
using Axl.Base.Models;

namespace Axl.Base.Core
{
    public class SnmpPc
    {
        private readonly ISnmpService _snmpService;

        public SnmpPc(ISnmpService snmpService)
        {
            _snmpService = snmpService ?? throw new ArgumentNullException(nameof(snmpService));
        }

        public Performance GetPerformance(string deviceId, string ip, int port, int version,
            string comm = "public", string user = "", string password = "", string privacy = "",
            int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
        {
            var perf = new Performance();
            string error = "";

            // 1. Obtener carga de CPU mediante HOST-RESOURCES-MIB
            // OID para hrProcessorLoad (1.3.6.1.2.1.25.3.3.1.2)
            var cpuWalk = _snmpService.Walk(deviceId, ip, port, version, "1.3.6.1.2.1.25.3.3.1.2",
                out error, comm, user, password, privacy, to, demo, authProto, privProto);

            var prcList = new List<Dictionary<string, string>>();
            if (string.IsNullOrEmpty(error) && cpuWalk != null)
            {
                foreach (var kvp in cpuWalk)
                {
                    var parts = kvp.Key.Split('.');
                    var id = parts.LastOrDefault();
                    var carga = kvp.Value;
                    prcList.Add(new Dictionary<string, string> { { "id", id }, { "carga", carga } });
                }
            }
            perf.AddPRC(prcList);

            // 2. Obtener almacenamiento (RAM y HDD) mediante HOST-RESOURCES-MIB
            // OID para hrStorageEntry (1.3.6.1.2.1.25.2.3.1)
            var storageWalk = _snmpService.Walk(deviceId, ip, port, version, "1.3.6.1.2.1.25.2.3.1",
                out error, comm, user, password, privacy, to, demo, authProto, privProto);

            var ramList = new List<Dictionary<string, string>>();
            var hddList = new List<Dictionary<string, string>>();

            if (string.IsNullOrEmpty(error) && storageWalk != null)
            {
                var storageDict = new Dictionary<string, Dictionary<string, string>>();

                // OID prefixes para las diferentes propiedades en hrStorageEntry
                var typePrefix = "1.3.6.1.2.1.25.2.3.1.2";    // hrStorageType
                var descPrefix = "1.3.6.1.2.1.25.2.3.1.3";    // hrStorageDescr
                var allocUnitPrefix = "1.3.6.1.2.1.25.2.3.1.4"; // hrStorageAllocationUnits
                var sizePrefix = "1.3.6.1.2.1.25.2.3.1.5";    // hrStorageSize
                var usedPrefix = "1.3.6.1.2.1.25.2.3.1.6";    // hrStorageUsed

                foreach (var kvp in storageWalk)
                {
                    var parts = kvp.Key.Split('.');
                    if (parts.Length < 1) continue;
                    
                    var index = parts.Last();

                    if (!storageDict.ContainsKey(index))
                        storageDict[index] = new Dictionary<string, string>();

                    if (kvp.Key.StartsWith(typePrefix)) storageDict[index]["type"] = kvp.Value;
                    else if (kvp.Key.StartsWith(descPrefix)) storageDict[index]["desc"] = kvp.Value;
                    else if (kvp.Key.StartsWith(allocUnitPrefix)) storageDict[index]["alloc"] = kvp.Value;
                    else if (kvp.Key.StartsWith(sizePrefix)) storageDict[index]["size"] = kvp.Value;
                    else if (kvp.Key.StartsWith(usedPrefix)) storageDict[index]["used"] = kvp.Value;
                }

                foreach (var kvp in storageDict)
                {
                    var data = kvp.Value;
                    
                    // Asegurar que las llaves esenciales existan
                    if (!data.ContainsKey("type") || !data.ContainsKey("size") || !data.ContainsKey("used") || !data.ContainsKey("alloc"))
                        continue;

                    string typeStr = data["type"];
                    double allocUnits = double.TryParse(data["alloc"], out var aU) ? aU : 1;
                    double sizeUnits = double.TryParse(data["size"], out var sU) ? sU : 0;
                    double usedUnits = double.TryParse(data["used"], out var uU) ? uU : 0;
                    string desc = data.ContainsKey("desc") ? data["desc"] : "Unknown";

                    // Convertir el tamaño a la estructura que espera la clase:
                    // Por lo general, los valores son provistos en Bytes absolutos, los modelos lo procesaran as-is en Performance o puede que debamos transformar de acuerdo al uso en la UI, pero Bytes funciona para el raw capacity.
                    double totalBytes = sizeUnits * allocUnits;
                    double usedBytes = usedUnits * allocUnits;
                    double freeBytes = totalBytes - usedBytes;

                    // Tipo para RAM Fisica (hrStorageRam): 1.3.6.1.2.1.25.2.1.2
                    if (typeStr.Contains("1.3.6.1.2.1.25.2.1.2"))
                    {
                        ramList.Add(new Dictionary<string, string>
                        {
                            { "instalada", totalBytes.ToString() },
                            { "libre", freeBytes.ToString() }
                        });
                    }
                    // Tipo para Disco Duro (hrStorageFixedDisk): 1.3.6.1.2.1.25.2.1.4
                    else if (typeStr.Contains("1.3.6.1.2.1.25.2.1.4"))
                    {
                        hddList.Add(new Dictionary<string, string>
                        {
                            { "nombre", desc },
                            { "capacidad", totalBytes.ToString() },
                            { "libre", freeBytes.ToString() }
                        });
                    }
                }
            }

            // Agregamos las estructuras completadas al objeto de Performance
            perf.AddRAM(ramList);
            perf.AddHDD(hddList);

            return perf;
        }
    }
}

