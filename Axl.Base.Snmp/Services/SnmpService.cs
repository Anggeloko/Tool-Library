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
            UdpTarget target = null;
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
                target = new UdpTarget((IPAddress)agent, port, to, 3);
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
                        if (result.Pdu.ErrorStatus == 2) r[o] = "SNMP No-Such-Object";
                        else if (result.Pdu.ErrorStatus != 0)
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
            }
            catch (Exception ex)
            {
                error = "[ERROR(SNMP)] GET Version 1: " + ex.Message + " [Ip: " + ip + "] [Port: " + port + "]";
            }
            finally { if (target != null) target.Close(); }
            return r;
        }
        private Dictionary<string, string> ExecV2(string ip, int port, List<string> oids, PduType type, out string error, int to, string comm)
        {
            error = "";
            Dictionary<string, string> r = new Dictionary<string, string>();
            UdpTarget target = null;
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
                target = new UdpTarget((IPAddress)agent, port, to, 3);
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
            }
            catch (Exception ex)
            {
                error = "[ERROR(SNMP)] GET Version 2: " + ex.Message + " [Ip: " + ip + "] [Port: " + port + "]";
            }
            finally { if (target != null) target.Close(); }
            return r;
        }
        private Dictionary<string, string> ExecV3(string ip, int port, List<string> oids, PduType type, out string error, int to, string usr, string psw, string prv, string comm, string authProto, string privProto)
        {
            error = "";
            var values = new Dictionary<string, string>();
            UdpTarget target = null;
            try
            {
                if (string.IsNullOrEmpty(usr) || string.IsNullOrEmpty(psw) || string.IsNullOrEmpty(prv))
                    throw new ArgumentException("configuration_invalid: User, password and privacy key are required");
                target = new UdpTarget((IPAddress)new IpAddress(ip), port, to, 3);
                var parameters = new SecureAgentParameters();
                if (!target.Discovery(parameters)) { error = "Discovery failed: no response from SNMP agent"; return values; }
                parameters.ContextName.Set(comm ?? "");
                parameters.authPriv(usr, authProto.ToUpperInvariant() == "MD5" ? AuthenticationDigests.MD5 : AuthenticationDigests.SHA1, psw, GetPrivacyProtocol(privProto), prv);
                var request = new Pdu(type);
                foreach (var oid in oids) request.VbList.Add(oid);
                var response = (SnmpV3Packet)target.Request(request, parameters);
                if (response == null) error = "No response received from SNMP agent";
                else if (response.ScopedPdu.Type == PduType.Report) error = ReportError(response.ScopedPdu);
                else if (response.ScopedPdu.ErrorStatus != 0)
                    error = SnmpError.ErrorMessage(response.ScopedPdu.ErrorStatus) + " at index " + response.ScopedPdu.ErrorIndex;
                else foreach (Vb binding in response.ScopedPdu.VbList) values[binding.Oid.ToString()] = binding.Value.ToString();
            }
            catch (Exception ex) { error = ex.Message; }
            finally { if (target != null) target.Close(); }
            return values;
        }

        internal static string ReportError(Pdu report)
        {
            var details = new List<string>();
            foreach (Vb binding in report.VbList)
            {
                var oid = binding.Oid.ToString();
                var cause = oid == "1.3.6.1.6.3.15.1.1.3.0" ? "authentication_failed: unknownUserName" :
                    oid == "1.3.6.1.6.3.15.1.1.5.0" ? "authentication_failed: wrongDigest" :
                    oid == "1.3.6.1.6.3.15.1.1.6.0" ? "authentication_failed: decryptionError" :
                    oid == "1.3.6.1.6.3.15.1.1.2.0" ? "notInTimeWindow" :
                    oid == "1.3.6.1.6.3.15.1.1.4.0" ? "unknownEngineID" :
                    oid == "1.3.6.1.6.3.15.1.1.1.0" ? "unsupportedSecurityLevel" : "protocol_error";
                details.Add(cause + " (" + oid + ")");
            }
            return "SNMPv3 REPORT: " + (details.Count == 0 ? "empty report" : string.Join("; ", details));
        }

        private Dictionary<string, string> WalkV1(string ip, int port, string oid, out string error, int to, string comm)
        { return WalkCore(ip, port, oid, 1, out error, to, "", "", "", comm, "SHA1", "DES"); }
        private Dictionary<string, string> WalkV2(string ip, int port, string oid, out string error, int to, string comm)
        { return WalkCore(ip, port, oid, 2, out error, to, "", "", "", comm, "SHA1", "DES"); }
        private Dictionary<string, string> WalkV3(string ip, int port, string oid, out string error, int to, string usr, string psw, string prv, string comm, string authProto, string privProto)
        { return WalkCore(ip, port, oid, 3, out error, to, usr, psw, prv, comm, authProto, privProto); }

        private Dictionary<string, string> WalkCore(string ip, int port, string oid, int version, out string error, int timeout, string user, string password, string privacy, string community, string authProto, string privProto)
        {
            error = "";
            var values = new Dictionary<string, string>();
            UdpTarget target = null;
            try
            {
                var root = new Oid(oid);
                var cursor = new Oid(oid);
                target = new UdpTarget((IPAddress)new IpAddress(ip), port, timeout, 3);
                var parameters = new AgentParameters(new OctetString(community ?? "")) { Version = version == 1 ? SnmpVersion.Ver1 : SnmpVersion.Ver2 };
                var secure = new SecureAgentParameters();
                if (version == 3)
                {
                    if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(privacy))
                        throw new ArgumentException("configuration_invalid: User, password and privacy key are required");
                    if (!target.Discovery(secure)) { error = "Discovery failed: no response from SNMP agent"; return values; }
                    secure.ContextName.Set(community ?? "");
                    secure.authPriv(user, authProto.ToUpperInvariant() == "MD5" ? AuthenticationDigests.MD5 : AuthenticationDigests.SHA1, password, GetPrivacyProtocol(privProto), privacy);
                }
                while (cursor != null)
                {
                    var request = new Pdu(version == 1 ? PduType.GetNext : PduType.GetBulk);
                    if (version != 1) { request.NonRepeaters = 0; request.MaxRepetitions = 5; }
                    request.VbList.Add(cursor);
                    Pdu response;
                    if (version == 3) { var packet = (SnmpV3Packet)target.Request(request, secure); response = packet == null ? null : packet.ScopedPdu; }
                    else if (version == 1) { var packet = (SnmpV1Packet)target.Request(request, parameters); response = packet == null ? null : packet.Pdu; }
                    else { var packet = (SnmpV2Packet)target.Request(request, parameters); response = packet == null ? null : packet.Pdu; }
                    if (response == null) { error = "No response received from SNMP agent"; break; }
                    if (response.Type == PduType.Report) { error = ReportError(response); break; }
                    // SNMPv1 reports noSuchName to indicate the end of a subtree.
                    if (version == 1 && response.ErrorStatus == 2) break;
                    if (response.ErrorStatus != 0) { error = SnmpError.ErrorMessage(response.ErrorStatus) + " at index " + response.ErrorIndex; break; }
                    if (response.VbList.Count == 0) break;
                    foreach (Vb binding in response.VbList)
                    {
                        if (!root.IsRootOf(binding.Oid) || binding.Value.Type == SnmpConstants.SMI_ENDOFMIBVIEW ||
                            binding.Value.ToString().IndexOf("No-Such", StringComparison.OrdinalIgnoreCase) >= 0)
                        { cursor = null; break; }
                        if (!OidAdvances(cursor.ToString(), binding.Oid.ToString()))
                        { error = "protocol_error: SNMP walk did not advance"; cursor = null; break; }
                        values[binding.Oid.ToString()] = binding.Value.ToString();
                        cursor = binding.Oid;
                    }
                }
            }
            catch (Exception ex) { error = ex.Message; }
            finally { if (target != null) target.Close(); }
            return values;
        }

        internal static bool OidAdvances(string previous, string next)
        {
            var left = previous.Trim('.').Split('.');
            var right = next.Trim('.').Split('.');
            for (int i = 0; i < Math.Min(left.Length, right.Length); i++)
            {
                var comparison = ulong.Parse(right[i]).CompareTo(ulong.Parse(left[i]));
                if (comparison != 0) return comparison > 0;
            }
            return right.Length > left.Length;
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


