﻿using System;
using System.Collections.Generic;
using System.Linq;
using Axl.Base.Interfaces;
using Axl.Base.Models;

namespace Axl.Base.Core
{
    public class SnmpRouter
    {
        private readonly ISnmpService _snmpService;

        public SnmpRouter(ISnmpService snmpService)
        {
            _snmpService = snmpService ?? throw new ArgumentNullException(nameof(snmpService));
        }

        public Router GetInfo(string deviceId, string ip, int port, int version,
            string comm = "public", string user = "", string password = "", string privacy = "",
            int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
        {
            var router = new Router();
            string error = "";

            // 1. OBTENER SYSTEM NAME (sysName = 1.3.6.1.2.1.1.5.0)
            var sysNameGet = _snmpService.Get(deviceId, ip, port, version, new List<string> { "1.3.6.1.2.1.1.5.0" },
                out error, comm, user, password, privacy, to, demo, authProto, privProto);

            if (string.IsNullOrEmpty(error) && sysNameGet != null && sysNameGet.ContainsKey("1.3.6.1.2.1.1.5.0"))
            {
                router.SetSystemName(sysNameGet["1.3.6.1.2.1.1.5.0"]);
            }

            // 2. OBTENER INTERFACES (ifEntry = 1.3.6.1.2.1.2.2.1)
            var interfacesWalk = _snmpService.Walk(deviceId, ip, port, version, "1.3.6.1.2.1.2.2.1",
                out error, comm, user, password, privacy, to, demo, authProto, privProto);

            var interfaceList = new List<Dictionary<string, string>>();

            if (string.IsNullOrEmpty(error) && interfacesWalk != null)
            {
                var interfacesDict = new Dictionary<string, Dictionary<string, string>>();

                // OID prefixes
                var descPrefix = "1.3.6.1.2.1.2.2.1.2";       // ifDescr
                var typePrefix = "1.3.6.1.2.1.2.2.1.3";       // ifType
                var adminStatusPrefix = "1.3.6.1.2.1.2.2.1.7"; // ifAdminStatus
                var operStatusPrefix = "1.3.6.1.2.1.2.2.1.8";  // ifOperStatus
                var inOctetsPrefix = "1.3.6.1.2.1.2.2.1.10";   // ifInOctets
                var outOctetsPrefix = "1.3.6.1.2.1.2.2.1.16";  // ifOutOctets

                foreach (var kvp in interfacesWalk)
                {
                    var parts = kvp.Key.Split('.');
                    if (parts.Length < 1) continue;
                    
                    var index = parts.Last();

                    if (!interfacesDict.ContainsKey(index))
                    {
                        interfacesDict[index] = new Dictionary<string, string> { { "index", index } };
                    }

                    if (kvp.Key.StartsWith(descPrefix)) interfacesDict[index]["descripcion"] = kvp.Value;
                    else if (kvp.Key.StartsWith(typePrefix)) interfacesDict[index]["tipo"] = kvp.Value;
                    else if (kvp.Key.StartsWith(adminStatusPrefix)) interfacesDict[index]["adminStatus"] = kvp.Value;
                    else if (kvp.Key.StartsWith(operStatusPrefix)) interfacesDict[index]["operStatus"] = kvp.Value;
                    else if (kvp.Key.StartsWith(inOctetsPrefix)) interfacesDict[index]["inOctets"] = kvp.Value;
                    else if (kvp.Key.StartsWith(outOctetsPrefix)) interfacesDict[index]["outOctets"] = kvp.Value;
                }

                foreach (var kvp in interfacesDict)
                {
                    // Añadimos la interfaz a la lista preparada
                    var data = kvp.Value;
                    interfaceList.Add(data);
                }
            }

            router.AddInterfaces(interfaceList);

            return router;
        }
    }
}

