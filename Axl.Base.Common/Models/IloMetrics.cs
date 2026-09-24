using System;
using System.Collections.Generic;

namespace Axl.Base.Models
{
    /// <summary>
    /// Modelo de datos estándar para métricas de hardware de servidores (HPE iLO, Dell iDRAC, etc.).
    /// </summary>
    public class IloMetrics
    {
        public DateTime Timestamp { get; set; }
        public string ServerIp { get; set; }

        // Métricas de energía (Power)
        public double? PowerWatts { get; set; }
        public double? Psu1Watts { get; set; }
        public double? Psu1AvgWatts { get; set; }
        public double? Psu2Watts { get; set; }
        public double? Psu2AvgWatts { get; set; }
        public double? VoltageIn { get; set; }

        // Métricas térmicas (Thermal)
        public double? InletTempC { get; set; }
        public double? Cpu1TempC { get; set; }
        public double? Cpu2TempC { get; set; }
        public double? HdMaxTempC { get; set; }
        public double? FanAvgPct { get; set; }

        // Diagnóstico / Memoria
        public string DimmHealth { get; set; }

        // Métricas Adicionales (Redfish / SNMP)
        public string SystemHealthRollup { get; set; }
        public string PowerState { get; set; }
        public string FirmwareVersion { get; set; }
        public string StorageHealth { get; set; }
        public List<string> Processors { get; set; }
        public List<string> NetworkInterfaces { get; set; }
        public List<string> LatestEvents { get; set; }

        // Métricas de salud / porcentajes
        public double? ExhaustTempC { get; set; }
        public double? CpuAvgFreqMhz { get; set; }
        public double? CpuUtilPct { get; set; }
        public double? DriveHealth { get; set; }
        public double? DimmHealthValue { get; set; }

        // Detalles adicionales
        public Dictionary<string, object> RawDetails { get; set; }

        public IloMetrics()
        {
            Timestamp = DateTime.UtcNow;
            ServerIp = string.Empty;
            DimmHealth = "OK";
            SystemHealthRollup = "OK";
            PowerState = "Unknown";
            FirmwareVersion = "Unknown";
            StorageHealth = "OK";
            Processors = new List<string>();
            NetworkInterfaces = new List<string>();
            LatestEvents = new List<string>();
            RawDetails = new Dictionary<string, object>();
        }
    }
}
