﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    public interface ISnmpService
    {
        Dictionary<string, string> Get(string deviceId, string ip, int port, int version, List<string> oids, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128");
        Dictionary<string, string> Walk(string deviceId,string ip, int port, int version, string oid, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128");
        Dictionary<int, int> InterpretIfTypes(Dictionary<string, string> datas, out Dictionary<int, ifTypeEl> dict);
    }
}

