using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Axl.Base.Ilo.Domain.Models;
using Axl.Base.Ilo.Domain.Ports;

namespace Axl.Base.Ilo.Infrastructure.Adapters
{
    /// <summary>
    /// Adaptador de Infraestructura (Outbound Adapter) para la API Redfish de HPE iLO.
    /// Implementa el puerto IIloService definido por el Dominio.
    /// </summary>
    public class IloRedfishAdapter : IIloService
    {
        private readonly JavaScriptSerializer _serializer;

        public IloRedfishAdapter()
        {
            _serializer = new JavaScriptSerializer();
        }

        public IloMetrics GetMetrics(string ip, string username, string password)
        {
            IloMetrics metrics = new IloMetrics();
            metrics.ServerIp = ip;
            metrics.Timestamp = DateTime.UtcNow;

            string baseUrl = ip.StartsWith("http") ? ip : "https://" + ip;
            baseUrl = baseUrl.TrimEnd('/');

            string authInfo = username + ":" + password;
            string authHeaderValue = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authInfo));

            // Helper para peticiones HTTP
            Func<string, Dictionary<string, object>> getRedfishEndpoint = (endpoint) =>
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl + endpoint);
                    request.Headers[HttpRequestHeader.Authorization] = authHeaderValue;
                    request.Accept = "application/json";
                    request.Method = "GET";
                    request.Timeout = 8000; // Timeout de 8 segundos por llamada

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (Stream stream = response.GetResponseStream())
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string json = reader.ReadToEnd();
                                return _serializer.Deserialize<Dictionary<string, object>>(json);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    metrics.RawDetails["Error_" + endpoint.Replace('/', '_')] = ex.Message;
                }
                return null;
            };

            // 1. Energía (Power)
            var powerData = getRedfishEndpoint("/redfish/v1/Chassis/1/Power");
            if (powerData != null) ParsePowerMetrics(powerData, metrics);

            // 2. Térmicas (Thermal)
            var thermalData = getRedfishEndpoint("/redfish/v1/Chassis/1/Thermal");
            if (thermalData != null) ParseThermalMetrics(thermalData, metrics);

            // 3. Sistema (Systems/1)
            var systemData = getRedfishEndpoint("/redfish/v1/Systems/1");
            if (systemData != null) ParseSystemMetrics(systemData, metrics);

            // 4. Administrador de iLO (Managers/1 - versión de firmware)
            var managerData = getRedfishEndpoint("/redfish/v1/Managers/1");
            if (managerData != null) ParseManagerMetrics(managerData, metrics);

            // 5. Procesadores (Processors)
            var processorsData = getRedfishEndpoint("/redfish/v1/Systems/1/Processors");
            if (processorsData != null) ParseProcessors(processorsData, getRedfishEndpoint, metrics);

            // 6. Almacenamiento (Storage)
            var storageData = getRedfishEndpoint("/redfish/v1/Systems/1/Storage");
            if (storageData != null) ParseStorage(storageData, getRedfishEndpoint, metrics);

            // 7. Interfaces de Red (NetworkInterfaces)
            var networkData = getRedfishEndpoint("/redfish/v1/Systems/1/NetworkInterfaces");
            if (networkData != null) ParseNetwork(networkData, getRedfishEndpoint, metrics);

            // 8. Eventos de Hardware IML (Integrated Management Log)
            var imlData = getRedfishEndpoint("/redfish/v1/Systems/1/LogServices/IML/Entries");
            if (imlData != null) ParseEvents(imlData, metrics);

            return metrics;
        }

        private void ParsePowerMetrics(Dictionary<string, object> data, IloMetrics metrics)
        {
            if (data.ContainsKey("PowerControl"))
            {
                var powerControl = data["PowerControl"] as ArrayList;
                if (powerControl != null && powerControl.Count > 0)
                {
                    var mainPower = powerControl[0] as Dictionary<string, object>;
                    if (mainPower != null && mainPower.ContainsKey("PowerConsumedWatts"))
                    {
                        metrics.PowerWatts = Convert.ToDouble(mainPower["PowerConsumedWatts"]);
                    }
                }
            }

            if (data.ContainsKey("PowerSupplies"))
            {
                var powerSupplies = data["PowerSupplies"] as ArrayList;
                if (powerSupplies != null)
                {
                    for (int i = 0; i < powerSupplies.Count; i++)
                    {
                        var psu = powerSupplies[i] as Dictionary<string, object>;
                        if (psu != null)
                        {
                            if (psu.ContainsKey("LastPowerOutputWatts"))
                            {
                                double watts = Convert.ToDouble(psu["LastPowerOutputWatts"]);
                                if (i == 0) metrics.Psu1Watts = watts;
                                else if (i == 1) metrics.Psu2Watts = watts;
                            }
                            if (psu.ContainsKey("AveragePowerOutputWatts"))
                            {
                                double avgWatts = Convert.ToDouble(psu["AveragePowerOutputWatts"]);
                                if (i == 0) metrics.Psu1AvgWatts = avgWatts;
                                else if (i == 1) metrics.Psu2AvgWatts = avgWatts;
                            }
                            if (metrics.VoltageIn == null && psu.ContainsKey("LineInputVoltage"))
                            {
                                metrics.VoltageIn = Convert.ToDouble(psu["LineInputVoltage"]);
                            }
                        }
                    }
                }
            }
        }

        private void ParseThermalMetrics(Dictionary<string, object> data, IloMetrics metrics)
        {
            if (data.ContainsKey("Temperatures"))
            {
                var temperatures = data["Temperatures"] as ArrayList;
                if (temperatures != null)
                {
                    double maxHdTemp = 0;
                    foreach (var entry in temperatures)
                    {
                        var tempSensor = entry as Dictionary<string, object>;
                        if (tempSensor != null && tempSensor.ContainsKey("Name") && tempSensor.ContainsKey("ReadingCelsius"))
                        {
                            string name = Convert.ToString(tempSensor["Name"]);
                            double val = Convert.ToDouble(tempSensor["ReadingCelsius"]);

                            if (name.IndexOf("Inlet", StringComparison.OrdinalIgnoreCase) >= 0) metrics.InletTempC = val;
                            else if (name.IndexOf("CPU 1", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Proc 1", StringComparison.OrdinalIgnoreCase) >= 0) metrics.Cpu1TempC = val;
                            else if (name.IndexOf("CPU 2", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Proc 2", StringComparison.OrdinalIgnoreCase) >= 0) metrics.Cpu2TempC = val;
                            else if (name.IndexOf("Exhaust", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Outlet", StringComparison.OrdinalIgnoreCase) >= 0) metrics.ExhaustTempC = val;
                            else if (name.IndexOf("HD Max", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Drive", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                if (val > maxHdTemp) maxHdTemp = val;
                            }
                        }
                    }
                    if (maxHdTemp > 0) metrics.HdMaxTempC = maxHdTemp;
                }
            }

            if (data.ContainsKey("Fans"))
            {
                var fans = data["Fans"] as ArrayList;
                if (fans != null && fans.Count > 0)
                {
                    double totalPct = 0;
                    int count = 0;
                    foreach (var entry in fans)
                    {
                        var fan = entry as Dictionary<string, object>;
                        if (fan != null && fan.ContainsKey("Reading"))
                        {
                            totalPct += Convert.ToDouble(fan["Reading"]);
                            count++;
                        }
                    }
                    if (count > 0) metrics.FanAvgPct = Math.Round(totalPct / count, 2);
                }
            }
        }

        private void ParseSystemMetrics(Dictionary<string, object> data, IloMetrics metrics)
        {
            if (data.ContainsKey("PowerState"))
            {
                metrics.PowerState = Convert.ToString(data["PowerState"]);
            }

            if (data.ContainsKey("Status"))
            {
                var status = data["Status"] as Dictionary<string, object>;
                if (status != null)
                {
                    if (status.ContainsKey("HealthRollup")) metrics.SystemHealthRollup = Convert.ToString(status["HealthRollup"]);
                    else if (status.ContainsKey("Health")) metrics.SystemHealthRollup = Convert.ToString(status["Health"]);

                    if (status.ContainsKey("Health"))
                    {
                        metrics.DimmHealth = Convert.ToString(status["Health"]);
                        if (metrics.DimmHealth == "OK") metrics.DimmHealthValue = 1.0;
                        else if (metrics.DimmHealth == "Warning") metrics.DimmHealthValue = 0.5;
                        else metrics.DimmHealthValue = 0.0;
                    }
                }
            }

            if (data.ContainsKey("Oem"))
            {
                var oem = data["Oem"] as Dictionary<string, object>;
                if (oem != null && oem.ContainsKey("Hp"))
                {
                    var hp = oem["Hp"] as Dictionary<string, object>;
                    if (hp != null && hp.ContainsKey("CpuUtilization"))
                    {
                        metrics.CpuUtilPct = Convert.ToDouble(hp["CpuUtilization"]);
                    }
                }
            }

            if (data.ContainsKey("Memory"))
            {
                var memory = data["Memory"] as Dictionary<string, object>;
                if (memory != null && memory.ContainsKey("@odata.id"))
                {
                    metrics.RawDetails["MemorySubsystemUrl"] = Convert.ToString(memory["@odata.id"]);
                }
            }
        }

        private void ParseManagerMetrics(Dictionary<string, object> data, IloMetrics metrics)
        {
            if (data.ContainsKey("FirmwareVersion"))
            {
                metrics.FirmwareVersion = Convert.ToString(data["FirmwareVersion"]);
            }
        }

        private void ParseProcessors(Dictionary<string, object> data, Func<string, Dictionary<string, object>> queryFunc, IloMetrics metrics)
        {
            if (data.ContainsKey("Members"))
            {
                var members = data["Members"] as ArrayList;
                if (members != null)
                {
                    double totalSpeed = 0;
                    int speedCount = 0;
                    foreach (var memberEntry in members)
                    {
                        var memberRef = memberEntry as Dictionary<string, object>;
                        if (memberRef != null && memberRef.ContainsKey("@odata.id"))
                        {
                            string url = Convert.ToString(memberRef["@odata.id"]);
                            var cpuDetail = queryFunc(url);
                            if (cpuDetail != null)
                            {
                                string name = cpuDetail.ContainsKey("Name") ? Convert.ToString(cpuDetail["Name"]) : "CPU";
                                string model = cpuDetail.ContainsKey("Model") ? Convert.ToString(cpuDetail["Model"]) : "Unknown Model";
                                string health = "Unknown";

                                if (cpuDetail.ContainsKey("Status"))
                                {
                                    var status = cpuDetail["Status"] as Dictionary<string, object>;
                                    if (status != null && status.ContainsKey("Health"))
                                    {
                                        health = Convert.ToString(status["Health"]);
                                    }
                                }
                                metrics.Processors.Add(name + ": " + model + " (Health: " + health + ")");

                                if (cpuDetail.ContainsKey("OperatingSpeedMHz"))
                                {
                                    totalSpeed += Convert.ToDouble(cpuDetail["OperatingSpeedMHz"]);
                                    speedCount++;
                                }
                                else if (cpuDetail.ContainsKey("MaxSpeedMHz"))
                                {
                                    totalSpeed += Convert.ToDouble(cpuDetail["MaxSpeedMHz"]);
                                    speedCount++;
                                }
                            }
                        }
                    }
                    if (speedCount > 0)
                    {
                        metrics.CpuAvgFreqMhz = totalSpeed / speedCount;
                    }
                }
            }
        }

        private void ParseStorage(Dictionary<string, object> data, Func<string, Dictionary<string, object>> queryFunc, IloMetrics metrics)
        {
            if (data.ContainsKey("Members"))
            {
                var members = data["Members"] as ArrayList;
                if (members != null && members.Count > 0)
                {
                    string worstHealth = "OK";
                    foreach (var memberEntry in members)
                    {
                        var memberRef = memberEntry as Dictionary<string, object>;
                        if (memberRef != null && memberRef.ContainsKey("@odata.id"))
                        {
                            string url = Convert.ToString(memberRef["@odata.id"]);
                            var storageDetail = queryFunc(url);
                            if (storageDetail != null && storageDetail.ContainsKey("Status"))
                            {
                                var status = storageDetail["Status"] as Dictionary<string, object>;
                                if (status != null && status.ContainsKey("Health"))
                                {
                                    string health = Convert.ToString(status["Health"]);
                                    if (health == "Critical") worstHealth = "Critical";
                                    else if (health == "Warning" && worstHealth != "Critical") worstHealth = "Warning";
                                }
                            }
                        }
                    }
                    metrics.StorageHealth = worstHealth;
                    if (worstHealth == "OK") metrics.DriveHealth = 1.0;
                    else if (worstHealth == "Warning") metrics.DriveHealth = 0.5;
                    else metrics.DriveHealth = 0.0;
                }
            }
        }

        private void ParseNetwork(Dictionary<string, object> data, Func<string, Dictionary<string, object>> queryFunc, IloMetrics metrics)
        {
            if (data.ContainsKey("Members"))
            {
                var members = data["Members"] as ArrayList;
                if (members != null)
                {
                    foreach (var memberEntry in members)
                    {
                        var memberRef = memberEntry as Dictionary<string, object>;
                        if (memberRef != null && memberRef.ContainsKey("@odata.id"))
                        {
                            string url = Convert.ToString(memberRef["@odata.id"]);
                            var nicDetail = queryFunc(url);
                            if (nicDetail != null)
                            {
                                string name = nicDetail.ContainsKey("Name") ? Convert.ToString(nicDetail["Name"]) : "NIC";
                                string statusStr = "Unknown";
                                if (nicDetail.ContainsKey("Status"))
                                {
                                    var status = nicDetail["Status"] as Dictionary<string, object>;
                                    if (status != null && status.ContainsKey("State"))
                                    {
                                        statusStr = Convert.ToString(status["State"]);
                                    }
                                }
                                metrics.NetworkInterfaces.Add(name + ": Status " + statusStr);
                            }
                        }
                    }
                }
            }
        }

        private void ParseEvents(Dictionary<string, object> data, IloMetrics metrics)
        {
            if (data.ContainsKey("Members"))
            {
                var members = data["Members"] as ArrayList;
                if (members != null)
                {
                    int count = 0;
                    foreach (var entry in members)
                    {
                        if (count >= 5) break; // Traer máximo los últimos 5 eventos
                        var log = entry as Dictionary<string, object>;
                        if (log != null && log.ContainsKey("Message"))
                        {
                            string message = Convert.ToString(log["Message"]);
                            string severity = log.ContainsKey("Severity") ? Convert.ToString(log["Severity"]) : "Info";
                            string created = log.ContainsKey("Created") ? Convert.ToString(log["Created"]) : string.Empty;

                            metrics.LatestEvents.Add("[" + severity + "] (" + created + "): " + message);
                            count++;
                        }
                    }
                }
            }
        }
    }
}
