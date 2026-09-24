using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    /// <summary>
    /// Contrato unificado para recolección de métricas de hardware de servidores (HPE iLO, Redfish, SNMP).
    /// </summary>
    public interface IIloService
    {
        /// <summary>
        /// Obtiene métricas de hardware de iLO mediante configuración completa (SNMP v1/v2c/v3 o Redfish).
        /// </summary>
        IloMetrics GetMetrics(
            string deviceId,
            string ip,
            int port = 0,
            int version = 3,
            string user = "",
            string password = "",
            string privacy = "",
            string comm = "",
            string authProto = "SHA1",
            string privProto = "DES",
            int timeoutMs = 2000);

        /// <summary>
        /// Sobrecarga simplificada para consultas Redfish tradicionales.
        /// </summary>
        IloMetrics GetMetrics(string ip, string username, string password);
    }
}
