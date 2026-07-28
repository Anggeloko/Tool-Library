using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Wmi.Services
{
    public class WmiService : IWmiService
    {
        private readonly object _syncLock = new object();
        private readonly int _delayms = 5;
        private readonly List<string> _localIPs;

        public WmiService()
        {
            // Cacheamos las IPs locales una vez para no repetir el proceso en cada lectura
            _localIPs = GetLocalIPs();
        }
        public List<Dictionary<string, string>> Read(string deviceId, string ip, string user, string password, WmiQuery wma, out string error, int port = 0)
        {
            error = "";
            var results = new List<Dictionary<string, string>>();

            if (user.ToLower() == "inhabilitado")
            {
                error = "[OPERACION(WMI)] Usuario no configurado para " + ip;
                return results;
            }

            bool lockAcquired = TrafficControl.StartExclusiveHeavy(deviceId);
            try
            {
                if (!lockAcquired)
                {
                    error = $"[BLOCK] Equip {deviceId} is busy (WMI). Lock timeout.";
                    return results;
                }

                ConnectionOptions options = CreateConnectionOptions(ip, user, password);
                string host = (port <= 0 || port > 65535) ? ip : $"{ip}:{port}";
                ManagementScope scope = new ManagementScope($@"\\{host}\{wma.Nmspace}", options);
                scope.Connect();

                string queryStr = BuildWqlQuery(wma);
                ObjectQuery query = new ObjectQuery(queryStr);

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                using (ManagementObjectCollection collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        var row = new Dictionary<string, string>();
                        foreach (var q in wma.Queries)
                        {
                            try
                            {
                                var val = obj[q.Value];
                                row.Add(q.Key, val?.ToString().Trim() ?? "");
                            }
                            catch { row.Add(q.Key, ""); }
                        }
                        results.Add(row);
                        obj.Dispose(); // Liberaci�n manual de cada objeto WMI
                    }
                }
            }
            catch (Exception ex)
            {
                error = $"[ERROR(WMI)] Read en {ip}: {ex.Message}";
            }
            finally
            {
                if (lockAcquired)
                {
                    System.Threading.Thread.Sleep(_delayms);
                    TrafficControl.StopExclusiveHeavy(deviceId);
                }
            }
            return results;
        }

        // --- M�TODOS DE APOYO PRIVADOS ---

        private ConnectionOptions CreateConnectionOptions(string ip, string user, string password)
        {
            // Si es local, no enviamos credenciales (evita errores de privilegio)
            if (_localIPs.Contains(ip) || ip.ToLower() == "localhost" || ip == "127.0.0.1")
            {
                return new ConnectionOptions();
            }

            return new ConnectionOptions
            {
                Username = user,
                Password = password,
                EnablePrivileges = true,
                Impersonation = ImpersonationLevel.Impersonate,
                Authentication = AuthenticationLevel.PacketPrivacy // Recomendado para WMI remoto moderno
            };
        }

        private string BuildWqlQuery(WmiQuery wma)
        {
            // Uso de string.Join para un c�digo m�s limpio en .NET 4.0
            string fields = string.Join(", ", wma.Queries.Select(x => x.Value).ToArray());
            return $"SELECT {fields} FROM {wma.CLspace}";
        }

        private List<string> GetLocalIPs()
        {
            var ips = new List<string>();
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                ips.AddRange(host.AddressList
                    .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                    .Select(x => x.ToString()));
            }
            catch { }
            return ips;
        }
    }
}


