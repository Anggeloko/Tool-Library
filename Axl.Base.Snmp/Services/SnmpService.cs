using SnmpSharpNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Snmp.Services
{
    public class SnmpService : ISnmpService
    {
        // Bloqueo a nivel de instancia: garantiza secuencialidad total
        private readonly int _delayms = 5;
        private readonly Dictionary<int, ifTypeEl> _ifTypeDefinitions;
        public SnmpService()
        {
            _ifTypeDefinitions = LoadIfTypes();
        }
        public Dictionary<string, string> Get(string deviceId, string ip, int port, int Version, List<string> oids, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
        {
            error = "";
            if (demo)
            {
                ip = "192.168.86.27";
                comm = Version == 3 ? "EPSON" : "public";
                user = "axl";
                password = "16845008Af";
                privacy = "16845008Af";
            }
            bool lockAcquired = TrafficControl.StartExclusiveHeavy(deviceId);

            try
            {
                if (!lockAcquired)
                {
                    error = $"[BLOCK] Equip {deviceId} isn't responding. Lock timeout.";
                    return new Dictionary<string, string>();
                }
                switch (Version)
                {
                    case 1: return ExecV1(ip, port, oids, PduType.Get, out error, to, comm);
                    case 2: return ExecV2(ip, port, oids, PduType.Get, out error, to, comm);
                    case 3: return ExecV3(ip, port, oids, PduType.Get, out error, to, user, password, privacy, comm, authProto, privProto);
                    default: error = "Version no soportada"; return new Dictionary<string, string>();
                }
            }
            finally
            {
                if (lockAcquired)
                {
                    System.Threading.Thread.Sleep(_delayms);
                    TrafficControl.StopExclusiveHeavy(deviceId);
                }
            }
        }
        public Dictionary<string, string> Walk(string deviceId, string ip, int port, int Version, string oid, out string error, string comm = "public", string user = "", string password = "", string privacy = "", int to = 500, bool demo = false, string authProto = "SHA1", string privProto = "AES128")
        {
            error = "";
            if (demo)
            {
                ip = "192.168.86.27";
                comm = Version == 3 ? "EPSON" : "public";
                user = "axl";
                password = "16845008Af";
                privacy = "16845008Af";
            }
            bool lockAcquired = TrafficControl.StartExclusiveHeavy(deviceId);
            try
            {
                if (!lockAcquired)
                {
                    error = $"[BLOCK] Equip {deviceId} isn't responding. Lock timeout.";
                    return new Dictionary<string, string>();
                }

                switch (Version)
                {
                    case 1: return WalkV1(ip, port, oid, out error, to, comm);
                    case 2: return WalkV2(ip, port, oid, out error, to, comm);
                    case 3: return WalkV3(ip, port, oid, out error, to, user, password, privacy, comm, authProto, privProto);
                    default: error = "Version no soportada"; return new Dictionary<string, string>();
                }
            }
            finally
            {
                if (lockAcquired)
                {
                    System.Threading.Thread.Sleep(_delayms);
                    TrafficControl.StopExclusiveHeavy(deviceId);
                }
            }
        }
        // --- M?todos de apoyo (Interpretaci?n de OIDs) ---
        public Dictionary<int, int> InterpretIfTypes(Dictionary<string, string> datas, out Dictionary<int, ifTypeEl> dict)
        {
            dict = _ifTypeDefinitions;
            var results = new Dictionary<int, int>();
            foreach (var d in datas)
            {
                if (d.Key.Contains("1.3.6.1.2.1.2.2.1.3"))
                {
                    var parts = d.Key.Split('.');
                    int prt, val;
                    if (int.TryParse(parts[parts.Length - 1], out prt) && int.TryParse(d.Value, out val))
                    {
                        if (_ifTypeDefinitions.ContainsKey(val) && !results.ContainsKey(prt))
                            results.Add(prt, val);
                    }
                }
            }
            return results;
        }
        // --- Implementaciones Privadas (V1, V2, V3) ---
        private Dictionary<string, string> ExecV1(string ip, int port, List<string> oids, PduType type, out string error, int to, string comm)
        {
            error = "";
            Dictionary<string, string> r = new Dictionary<string, string>();
            try
            {
                OctetString community = new OctetString(comm);
                // Define agent parameters class
                AgentParameters param = new AgentParameters(community);
                // Set SNMP Version to 1 (or 2)
                param.Version = SnmpVersion.Ver1;
                // Construct the agent address object
                // IpAddress class is easy to use here because
                //  it will try to resolve constructor parameter if it doesn't
                //  parse to an IP address
                IpAddress agent = new IpAddress(ip);
                // Construct target
                UdpTarget target = new UdpTarget((IPAddress)agent, port, to, 3);
                // Pdu class used for all requests
                foreach (string o in oids)
                {
                    Pdu pdu = new Pdu(type);
                    pdu.VbList.Add(o);
                    // Make SNMP request
                    SnmpV1Packet result = (SnmpV1Packet)target.Request(pdu, param);
                    // If result is null then agent didn't reply or we couldn't parse the reply.
                    if (result != null)
                    {
                        // ErrorStatus other then 0 is an error returned by 
                        // the Agent - see SnmpConstants for error definitions
                        if (result.Pdu.ErrorStatus != 0)
                        {
                            // agent reported an error with the request
                            error = "[ERROR(SNMP)] GET Version 1: " + result.Pdu.ErrorStatus + "(" + result.Pdu.ErrorIndex + ") [Ip: " + ip + "] [Port: " + port + "]";
                        }
                        else
                        {
                            foreach (Vb v in result.Pdu.VbList)
                            {
                                if (!r.ContainsKey(v.Oid.ToString()))
                                    r.Add(v.Oid.ToString(), v.Value.ToString());
                            }
                        }
                    }
                    else
                    {
                        error = "[ERROR(SNMP)] GET Version 1: No response received from SNMP agent [Ip: " + ip + "] [Port: " + port + "]";
                    }
                }
                target.Close();
            }
            catch (Exception ex)
            {
                error = "[ERROR(SNMP)] GET Version 1: " + ex.Message + " [Ip: " + ip + "] [Port: " + port + "]";
            }
            return r;
        }
        private Dictionary<string, string> ExecV2(string ip, int port, List<string> oids, PduType type, out string error, int to, string comm)
        {
            error = "";
            Dictionary<string, string> r = new Dictionary<string, string>();
            try
            {
                OctetString community = new OctetString(comm);
                // Define agent parameters class
                AgentParameters param = new AgentParameters(community);
                // Set SNMP Version to 1 (or 2)
                param.Version = SnmpVersion.Ver2;
                // Construct the agent address object
                // IpAddress class is easy to use here because
                //  it will try to resolve constructor parameter if it doesn't
                //  parse to an IP address
                IpAddress agent = new IpAddress(ip);
                // Construct target
                UdpTarget target = new UdpTarget((IPAddress)agent, port, to, 3);
                // Pdu class used for all requests
                Pdu pdu = new Pdu(type);
                foreach (string o in oids)
                {
                    pdu.VbList.Add(o);
                }
                // Make SNMP request
                SnmpV2Packet result = (SnmpV2Packet)target.Request(pdu, param);
                // If result is null then agent didn't reply or we couldn't parse the reply.
                if (result != null)
                {
                    // ErrorStatus other then 0 is an error returned by 
                    // the Agent - see SnmpConstants for error definitions
                    if (result.Pdu.ErrorStatus != 0)
                    {
                        // agent reported an error with the request
                        error = "[ERROR(SNMP)] GET Version 2: " + result.Pdu.ErrorStatus + "(" + result.Pdu.ErrorIndex + ") [Ip: " + ip + "] [Port: " + port + "]";
                    }
                    else
                    {
                        foreach (Vb v in result.Pdu.VbList)
                        {
                            if (!r.ContainsKey(v.Oid.ToString()))
                                r.Add(v.Oid.ToString(), v.Value.ToString());
                        }
                    }
                }
                else
                {
                    error = "[ERROR(SNMP)] GET Version 2: No response received from SNMP agent [Ip: " + ip + "] [Port: " + port + "]";
                }
                target.Close();
            }
            catch (Exception ex)
            {
                error = "[ERROR(SNMP)] GET Version 2: " + ex.Message + " [Ip: " + ip + "] [Port: " + port + "]";
            }
            return r;
        }
        private Dictionary<string, string> ExecV3(string ip, int port, List<string> oids, PduType type, out string error, int to, string usr, string psw, string prv, string comm, string authProto, string privProto)
        {
            error = "";
            Dictionary<string, string> r = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(usr) || string.IsNullOrEmpty(psw) || string.IsNullOrEmpty(prv))
            {
                error = $"[ERROR(SNMP)] GET Version 3: User, password and privacy key are required [Ip: {ip}] [Port: {port}]";
                return r;
            }
            try
            {
                IpAddress ipa = new IpAddress(ip);
                UdpTarget target = new UdpTarget((IPAddress)ipa, port, to, 3);
                SecureAgentParameters param = new SecureAgentParameters();
                if (!target.Discovery(param))
                {
                    error = $"[ERROR(SNMP)] GET Version 3: Discovery failed [Ip: {ip}] [Port: {port}]";
                    target.Close();
                    return r;
                }
                // Construct a Protocol Data Unit (PDU)
                Pdu pdu = new Pdu();
                // Set the request type (default is Get)
                pdu.Type = type;
                // Add Context Name
                param.ContextName.Set(comm);
                // Add OID
                foreach (string o in oids)
                {
                    pdu.VbList.Add(o);
                }
                // SecureAgentParameters class (see discovery section above)
                AuthenticationDigests auth = authProto.ToUpper() == "MD5" ? AuthenticationDigests.MD5 : AuthenticationDigests.SHA1;
                PrivacyProtocols priv = GetPrivacyProtocol(privProto);
                param.authPriv(usr, auth, psw, priv, prv);
                //param.authPriv(usr, AuthenticationDigests.SHA1, psw, PrivacyProtocols.AES128, prv);
                // Make a request. Request can throw a number of errors so wrap it in try/catch
                SnmpV3Packet result;
                try
                {
                    result = (SnmpV3Packet)target.Request(pdu, param);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                    result = null;
                }
                if (result != null)
                {
                    if (result.ScopedPdu.Type == PduType.Report)
                    {
                        foreach (Vb v in result.ScopedPdu.VbList)
                        {
                            //Log.GetInstance().Write("[REPORT(SNMP)] GET Version 3: " + v.Oid.ToString() + " -> (" + SnmpConstants.GetTypeName(v.Value.Type) + ") " + v.Value.ToString());
                        }
                    }
                    else
                    {
                        if (result.ScopedPdu.ErrorStatus == 0)
                        {
                            foreach (Vb v in result.ScopedPdu.VbList)
                            {
                                if (!r.ContainsKey(v.Oid.ToString()))
                                    r.Add(v.Oid.ToString(), v.Value.ToString());
                            }
                        }
                        else
                        {
                            error = "[ERROR(SNMP)] GET Version 3: " + SnmpError.ErrorMessage(result.ScopedPdu.ErrorStatus) + " " + result.ScopedPdu.ErrorStatus + ": " + result.ScopedPdu.ErrorIndex + " [Ip: " + ip + "] [Port: " + port + "]";
                        }
                    }
                }
                target.Close();
            }
            catch (Exception ex)
            {
                error = $"[ERROR(SNMP)] GET Version 3: {ex.Message} [Ip: {ip}] [Port: {port}]";
            }
            return r;
        }
        private Dictionary<string, string> WalkV1(string ip, int port, string oid, out string error, int to, string comm)
        {
            error = "";
            Dictionary<string, string> r = new Dictionary<string, string>();
            try
            {
                // SNMP community name
                OctetString community = new OctetString(comm);
                // Define agent parameters class
                AgentParameters param = new AgentParameters(community);
                // Set SNMP Version to 1
                param.Version = SnmpVersion.Ver1;
                // Construct the agent address object
                // IpAddress class is easy to use here because
                //  it will try to resolve constructor parameter if it doesn't
                //  parse to an IP address
                IpAddress agent = new IpAddress(ip);
                // Construct target
                UdpTarget target = new UdpTarget((IPAddress)agent, port, to, 3);
                // Define Oid that is the root of the MIB
                //  tree you wish to retrieve
                Oid rootOid = new Oid(oid); // ifDescr
                                            // This Oid represents last Oid returned by
                                            //  the SNMP agent
                Oid lastOid = (Oid)rootOid.Clone();
                // Pdu class used for all requests
                Pdu pdu = new Pdu(PduType.GetNext);
                // Loop through results
                while (lastOid != null)
                {
                    // When Pdu class is first constructed, RequestId is set to a random value
                    // that needs to be incremented on subsequent requests made using the
                    // same instance of the Pdu class.
                    if (pdu.RequestId != 0)
                    {
                        pdu.RequestId += 1;
                    }
                    // Clear Oids from the Pdu class.
                    pdu.VbList.Clear();
                    // Initialize request PDU with the last retrieved Oid
                    pdu.VbList.Add(lastOid);
                    // Make SNMP request
                    SnmpV1Packet result = (SnmpV1Packet)target.Request(pdu, param);
                    // You should catch exceptions in the Request if using in real application.

                    // If result is null then agent didn't reply or we couldn't parse the reply.
                    if (result != null)
                    {
                        // ErrorStatus other then 0 is an error returned by 
                        // the Agent - see SnmpConstants for error definitions
                        if (result.Pdu.ErrorStatus != 0)
                        {
                            // agent reported an error with the request
                            error = "[ERROR(SNMP)] WALK Version 1: " + result.Pdu.ErrorStatus + "(" + result.Pdu.ErrorIndex + ") [Ip: " + ip + "] [Port: " + port + "]";
                            lastOid = null;
                            break;
                        }
                        else
                        {
                            // Walk through returned variable bindings
                            foreach (Vb v in result.Pdu.VbList)
                            {

                                // Check that retrieved Oid is "child" of the root OID
                                if (rootOid.IsRootOf(v.Oid))
                                {
                                    if (!r.ContainsKey(v.Oid.ToString()))
                                        r.Add(v.Oid.ToString(), v.Value.ToString());
                                    lastOid = v.Oid;
                                }
                                else
                                {
                                    // we have reached the end of the requested
                                    // MIB tree. Set lastOid to null and exit loop
                                    lastOid = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        error = "[ERROR(SNMP)] WALK Version 1: No response received from SNMP agent [Ip: " + ip + "] [Port: " + port + "]";
                    }
                }
                target.Close();
            }
            catch (Exception ex)
            {
                error = "[ERROR(SNMP)] WALK Version 1: " + ex.Message + " [Ip: " + ip + "] [Port: " + port + "]";
            }
            return r;
        }
        private Dictionary<string, string> WalkV2(string ip, int port, string oid, out string error, int to, string comm)
        {
            error = "";
            Dictionary<string, string> r = new Dictionary<string, string>();
            try
            {
                // SNMP community name
                OctetString community = new OctetString(comm);
                // Define agent parameters class
                AgentParameters param = new AgentParameters(community);
                // Set SNMP Version to 2 (GET-BULK only works with SNMP ver 2 and 3)
                param.Version = SnmpVersion.Ver2;
                // Construct the agent address object
                // IpAddress class is easy to use here because
                //  it will try to resolve constructor parameter if it doesn't
                //  parse to an IP address
                IpAddress agent = new IpAddress(ip);
                // Construct target
                UdpTarget target = new UdpTarget((IPAddress)agent, port, to, 3);
                // Define Oid that is the root of the MIB
                //  tree you wish to retrieve
                Oid rootOid = new Oid(oid); // ifDescr
                                            // This Oid represents last Oid returned by
                                            //  the SNMP agent
                Oid lastOid = (Oid)rootOid.Clone();
                // Pdu class used for all requests
                Pdu pdu = new Pdu(PduType.GetBulk);
                // In this example, set NonRepeaters value to 0
                pdu.NonRepeaters = 0;
                // MaxRepetitions tells the agent how many Oid/Value pairs to return
                // in the response.
                pdu.MaxRepetitions = 5;
                // Loop through results
                while (lastOid != null)
                {
                    // When Pdu class is first constructed, RequestId is set to 0
                    // and during encoding id will be set to the random value
                    // for subsequent requests, id will be set to a value that
                    // needs to be incremented to have unique request ids for each
                    // packet
                    if (pdu.RequestId != 0)
                    {
                        pdu.RequestId += 1;
                    }
                    // Clear Oids from the Pdu class.
                    pdu.VbList.Clear();
                    // Initialize request PDU with the last retrieved Oid
                    pdu.VbList.Add(lastOid);
                    // Make SNMP request
                    SnmpV2Packet result = (SnmpV2Packet)target.Request(pdu, param);
                    // You should catch exceptions in the Request if using in real application.

                    // If result is null then agent didn't reply or we couldn't parse the reply.
                    if (result != null)
                    {
                        // ErrorStatus other then 0 is an error returned by 
                        // the Agent - see SnmpConstants for error definitions
                        if (result.Pdu.ErrorStatus != 0)
                        {
                            // agent reported an error with the request
                            error = "[ERROR(SNMP)] WALK Version 2: " + result.Pdu.ErrorStatus + "(" + result.Pdu.ErrorIndex + ") [Ip: " + ip + "] [Port: " + port + "]";
                            lastOid = null;
                            break;
                        }
                        else
                        {
                            // Walk through returned variable bindings
                            foreach (Vb v in result.Pdu.VbList)
                            {
                                // Check that retrieved Oid is "child" of the root OID
                                if (rootOid.IsRootOf(v.Oid))
                                {
                                    if (!r.ContainsKey(v.Oid.ToString()))
                                        r.Add(v.Oid.ToString(), v.Value.ToString());
                                    if (v.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW)
                                        lastOid = null;
                                    else
                                        lastOid = v.Oid;
                                }
                                else
                                {
                                    // we have reached the end of the requested
                                    // MIB tree. Set lastOid to null and exit loop
                                    lastOid = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        error = "[ERROR(SNMP)] WALK Version 2: No response received from SNMP agent [Ip: " + ip + "] [Port: " + port + "]";
                    }
                }
                target.Close();
            }
            catch (Exception ex)
            {
                error = "[ERROR(SNMP)] WALK Version 2: " + ex.Message + " [Ip: " + ip + "] [Port: " + port + "]";
            }
            return r;
        }
        private Dictionary<string, string> WalkV3(string ip, int port, string oid, out string error, int to, string usr, string psw, string prv, string comm, string authProto, string privProto)
        {
            error = "";
            Dictionary<string, string> r = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(usr) || string.IsNullOrEmpty(psw) || string.IsNullOrEmpty(prv))
            {
                error = $"[ERROR(SNMP)] GET Version 3: User, password and privacy key are required [Ip: {ip}] [Port: {port}]";
                return r;
            }
            try
            {
                IpAddress ipa = new IpAddress(ip);
                UdpTarget target = new UdpTarget((IPAddress)ipa, port, to, 3);
                SecureAgentParameters param = new SecureAgentParameters();
                if (!target.Discovery(param))
                {
                    error = $"[ERROR(SNMP)] GET Version 3: Discovery failed [Ip: {ip}] [Port: {port}]";
                    target.Close();
                    return r;
                }
                // Define Oid that is the root of the MIB
                //  tree you wish to retrieve
                Oid rootOid = new Oid(oid); // ifDescr
                                            // This Oid represents last Oid returned by
                                            //  the SNMP agent
                Oid lastOid = (Oid)rootOid.Clone();
                // Pdu class used for all requests
                Pdu pdu = new Pdu(PduType.GetBulk);
                // In this example, set NonRepeaters value to 0
                pdu.NonRepeaters = 0;
                // MaxRepetitions tells the agent how many Oid/Value pairs to return
                // in the response.
                pdu.MaxRepetitions = 5;
                // Add Context Name
                param.ContextName.Set(comm);
                // SecureAgentParameters class (see discovery section above)
                AuthenticationDigests auth = authProto.ToUpper() == "MD5" ? AuthenticationDigests.MD5 : AuthenticationDigests.SHA1;
                PrivacyProtocols priv = GetPrivacyProtocol(privProto);
                param.authPriv(usr, auth, psw, priv, prv);
                //param.authPriv(usr, AuthenticationDigests.SHA1, psw, PrivacyProtocols.AES128, prv);
                // Loop through results
                while (lastOid != null)
                {
                    // When Pdu class is first constructed, RequestId is set to 0
                    // and during encoding id will be set to the random value
                    // for subsequent requests, id will be set to a value that
                    // needs to be incremented to have unique request ids for each
                    // packet
                    if (pdu.RequestId != 0)
                    {
                        pdu.RequestId += 1;
                    }
                    // Clear Oids from the Pdu class.
                    pdu.VbList.Clear();
                    // Initialize request PDU with the last retrieved Oid
                    pdu.VbList.Add(lastOid);
                    // Make SNMP request
                    SnmpV3Packet result = (SnmpV3Packet)target.Request(pdu, param);
                    // You should catch exceptions in the Request if using in real application.

                    // If result is null then agent didn't reply or we couldn't parse the reply.
                    if (result != null)
                    {
                        // ErrorStatus other then 0 is an error returned by 
                        // the Agent - see SnmpConstants for error definitions
                        if (result.Pdu.ErrorStatus != 0)
                        {
                            // agent reported an error with the request
                            error = "[ERROR(SNMP)] WALK Version 3: " + result.Pdu.ErrorStatus + "(" + result.Pdu.ErrorIndex + ") [Ip: " + ip + "] [Port: " + port + "]";
                            lastOid = null;
                            break;
                        }
                        else
                        {
                            // Walk through returned variable bindings
                            foreach (Vb v in result.Pdu.VbList)
                            {
                                // Check that retrieved Oid is "child" of the root OID
                                if (rootOid.IsRootOf(v.Oid))
                                {
                                    if (!r.ContainsKey(v.Oid.ToString()))
                                        r.Add(v.Oid.ToString(), v.Value.ToString());
                                    if (v.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW)
                                        lastOid = null;
                                    else
                                        lastOid = v.Oid;
                                }
                                else
                                {
                                    // we have reached the end of the requested
                                    // MIB tree. Set lastOid to null and exit loop
                                    lastOid = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        error = "[ERROR(SNMP)] WALK Version 3: No response received from SNMP agent [Ip: " + ip + "] [Port: " + port + "]";
                    }
                }
                target.Close();
            }
            catch (Exception ex)
            {
                error = $"[ERROR(SNMP)] GET Version 3: {ex.Message} [Ip: {ip}] [Port: {port}]";
            }

            return r;
        }
        private Dictionary<int, ifTypeEl> LoadIfTypes() => new Dictionary<int, ifTypeEl>()
            {
                    { 1, new ifTypeEl() { Nombre = "other", Tipo = "Varios", Descripcion = "Puede abarcar tecnolog?as propietarias, experimentales o simplemente aquellas que no han sido estandarizadas." } },
                    { 2, new ifTypeEl() { Nombre = "regular1822", Tipo = "RFC1822", Descripcion = "Se refiere a la interfaz de una red ARPANET basada en la especificaci?n RFC 1822." } },
                    { 3, new ifTypeEl() { Nombre = "hdh1822", Tipo = "Host-to-Destination Header", Descripcion = "Interfaces HDH (Host-to-Destination Header) de redes ARPANET." } },
                    { 4, new ifTypeEl() { Nombre = "ddn-x25", Tipo = "DDNX25", Descripcion = "Interfaz para redes DDN (Defense Data Network) que utilizaban el protocolo X.25." } },
                    { 5, new ifTypeEl() { Nombre = "rfc877-x25", Tipo = "RFC877", Descripcion = "Interfaz para redes X.25 que cumplen con la especificaci?n RFC 877." } },
                    { 6, new ifTypeEl() { Nombre = "ethernet-csmacd", Tipo = "Fisico", Descripcion = "Interfaz Ethernet (CSMA/CD, lo m?s com?n).  Casi cualquier puerto Ethernet est?ndar (10BASE-T, 100BASE-TX, Gigabit Ethernet, etc.) caer? bajo este tipo." } },
                    { 7, new ifTypeEl() { Nombre = "iso88023-csmacd", Tipo = "Fisico", Descripcion = "Implementaci?n de Ethernet seg?n el est?ndar ISO 8802.3." } },
                    { 8, new ifTypeEl() { Nombre = "iso88024-tokenbus", Tipo = "TokenBus", Descripcion = "Interfaz para redes Token Bus, un tipo de red de ?rea local que utilizaba un esquema de paso de testigo." } },
                    { 9, new ifTypeEl() { Nombre = "iso88025-tokenring", Tipo = "TokenRing", Descripcion = "Interfaz para redes Token Ring, otro tipo de red de ?rea local que utilizaba un esquema de paso de testigo en una topolog?a de anillo." } },
                    { 10, new ifTypeEl() { Nombre = "iso88026-man", Tipo = "MAN", Descripcion = "Interfaz para redes MAN (Metropolitan Area Network) basadas en el est?ndar ISO 8802.6." } },
                    { 11, new ifTypeEl() { Nombre = "starlan", Tipo = "StarLAN", Descripcion = "Interfaz para redes StarLAN, una implementaci?n temprana de Ethernet que utilizaba topolog?a de estrella." } },
                    { 12, new ifTypeEl() { Nombre = "proteon-10nbit", Tipo = "Proteon10Mbps", Descripcion = "Interfaz para redes Proteon de 10 Mbps." } },
                    { 13, new ifTypeEl() { Nombre = "proteon-80mbit", Tipo = "Proteon80Mbps", Descripcion = "Interfaz para redes Proteon de 80 Mbps." } },
                    { 14, new ifTypeEl() { Nombre = "hyperchannel", Tipo = "Hyperchannel", Descripcion = "Interfaz para redes Hyperchannel, una tecnolog?a de red de alta velocidad utilizada en supercomputadoras." } },
                    { 15, new ifTypeEl() { Nombre = "fddi", Tipo = "Fibra", Descripcion = "Interfaz para redes FDDI (Fiber Distributed Data Interface), una tecnolog?a de red de ?rea local que utilizaba fibra ?ptica." } },
                    { 16, new ifTypeEl() { Nombre = "lapb", Tipo = "LAPB", Descripcion = "Interfaz para conexiones LAPB (Link Access Procedure Balanced), un protocolo de enlace de datos utilizado en X.25." } },
                    { 17, new ifTypeEl() { Nombre = "sdlc", Tipo = "SDLC", Descripcion = "Interfaz para conexiones SDLC (Synchronous Data Link Control), un protocolo de enlace de datos desarrollado por IBM." } },
                    { 18, new ifTypeEl() { Nombre = "ds1", Tipo = "DS1", Descripcion = "Interfaz para l?neas DS1 (Digital Signal 1), una l?nea telef?nica digital de 1.544 Mbps." } },
                    { 19, new ifTypeEl() { Nombre = "e1", Tipo = "E1", Descripcion = "Interfaz para l?neas E1, una l?nea telef?nica digital de 2.048 Mbps, utilizada en Europa." } },
                    { 20, new ifTypeEl() { Nombre = "basicisdn", Tipo = "BasicISDN", Descripcion = "Interfaz para ISDN b?sico (Integrated Services Digital Network), un servicio telef?nico digital que proporcionaba canales de voz y datos." } },
                    { 21, new ifTypeEl() { Nombre = "primaryisdn", Tipo = "PrimaryISDN", Descripcion = "Interfaz para ISDN primario, una Version de mayor capacidad de ISDN b?sico." } },
                    { 22, new ifTypeEl() { Nombre = "proppointtopointserial", Tipo = "PointToPointSerial", Descripcion = "Interfaz para conexiones seriales punto a punto propietarias." } },
                    { 23, new ifTypeEl() { Nombre = "ppp", Tipo = "PPP", Descripcion = "Interfaz para PPP (Point-to-Point Protocol), un protocolo de enlace de datos utilizado para conexiones directas entre dos nodos." } },
                    { 24, new ifTypeEl() { Nombre = "softwareloopback", Tipo = "Loopback", Descripcion = "Interfaz de loopback (virtual, usada para pruebas y direccionamiento interno)." } },
                    { 25, new ifTypeEl() { Nombre = "eon", Tipo = "EON", Descripcion = "No tiene un uso muy extendido, era parte de una tecnologia de redes de comunicaciones, hoy en d?a es obsoleto." } },
                    { 26, new ifTypeEl() { Nombre = "ethernet-3mbit", Tipo = "Fisico", Descripcion = "Una Version mas antigua de ethernet, que trabaja a una velocidad de 3 megabits por segundo, hoy en d?a es obsoleta." } },
                    { 27, new ifTypeEl() { Nombre = "nsip", Tipo = "NSIP", Descripcion = "Protocolo de internet nativo. No esta muy extendido su uso, en redes de datos actuales, y es de caracter experimental." } },
                    { 28, new ifTypeEl() { Nombre = "slip", Tipo = "SLIP", Descripcion = "Protocolo de internet de linea serial, antecesor del protocolo ppp. Es un protocolo de conexi?n serial, muy antiguo." } },
                    { 29, new ifTypeEl() { Nombre = "ultra", Tipo = "ULTRA", Descripcion = "No se encuentra informaci?n estandarizada al respecto de este valor en IANA, por lo cual es posible que su implementaci?n sea propietaria de algun fabricante de equipos de comunicaci?n, ? de caracter experimental." } },
                    { 30, new ifTypeEl() { Nombre = "ds3", Tipo = "DS3", Descripcion = "Es una linea de transmisi?n digital, que transmite datos a una velocidad de 44.736 Mbps. Usada en comunicaciones de alta velocidad." } },
                    { 31, new ifTypeEl() { Nombre = "sip", Tipo = "SIP", Descripcion = "Protocolo de inicio de sesi?n. Usado en comunicaciones de voz sobre IP (VoIP), video conferencias, y mensajer?a instantanea." } },
                    { 32, new ifTypeEl() { Nombre = "propvirtual", Tipo = "Virtual", Descripcion = "Interfaz virtual propietaria. Algunos fabricantes usan este tipo para interfaces virtuales internas." } },
                    { 37, new ifTypeEl() { Nombre = "atm", Tipo = "ATM", Descripcion = "Interfaz ATM (Asynchronous Transfer Mode)." } },
                    { 53, new ifTypeEl() { Nombre = "propmultiplexor", Tipo = "Multiplexor", Descripcion = "Interfaz de multiplexaci?n propietaria." } },
                    { 56, new ifTypeEl() { Nombre = "fo", Tipo = "Fibra", Descripcion = "Interfaz para redes con fibra ?ptica." } },
                    { 71, new ifTypeEl() { Nombre = "wifi", Tipo = "WiFi", Descripcion = "Interfaces inal?mbricas, de mucha importancia en los tiempos actuales." } },
                    { 117, new ifTypeEl() { Nombre = "gigabitethernet", Tipo = "Fisico ", Descripcion = "Interfaces Ethernet de 1 Gigabit por segundo." } },
                    { 131, new ifTypeEl() { Nombre = "tengigabitethernet", Tipo = "Fisico", Descripcion = "Interfaces Ethernet de 10 Gigabits por segundo." } },
                    { 132, new ifTypeEl() { Nombre = "tunnel", Tipo = "Tunel", Descripcion = "Interfaz de t?nel (usada para VPNs y otras conexiones encapsuladas)." } },
                    { 135, new ifTypeEl() { Nombre = "l2vlan", Tipo = "VLAN", Descripcion = "Interfaz VLAN de capa 2." } },
                    { 136, new ifTypeEl() { Nombre = "l3ipvlan", Tipo = "VLAN", Descripcion = "Interfaz VLAN de capa 3 (interfaz IP asociada a una VLAN)." } },
                    { 161, new ifTypeEl() { Nombre = "ieee8023adLag", Tipo = "LAG", Descripcion = "Interfaz LAG (Link Aggregation Group) definida en IEEE 802.3ad." } },
                    { 166, new ifTypeEl() { Nombre = "fortygigabitethernet ", Tipo = "Fisico", Descripcion = "Interfaces Ethernet de 40 gigabits por segundo." } },
                    { 209, new ifTypeEl() { Nombre = "stackedVlan", Tipo = "VLAN", Descripcion = "VLAN apilada." } },
                    { 243, new ifTypeEl() { Nombre = "hundredgigabitethernet", Tipo = "Fisico", Descripcion = "Interfaces Ethernet de 100 gigabits por segundo." } },
            };
        private PrivacyProtocols GetPrivacyProtocol(string protocol)
        {
            switch (protocol.ToUpper())
            {
                case "DES": return PrivacyProtocols.DES;
                case "AES192": return PrivacyProtocols.AES192;
                case "AES256": return PrivacyProtocols.AES256;
                case "AES128":
                default: return PrivacyProtocols.AES128;
            }
        }
    }
}


