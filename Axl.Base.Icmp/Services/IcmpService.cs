﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Icmp.Services
{
    public class IcmpService : IIcmpService
    {
        private readonly int _delayms = 5;

        // Est�ndar completo: 10 intentos, 300ms de timeout para mayor fluidez
        public PingElement Read(string deviceId, string ip, out string error, int timeout = 300, int attempts = 10)
        {
            bool isHeavy = attempts >= 5;
            bool lockAcquired = isHeavy ? TrafficControl.StartExclusiveHeavy(deviceId)
                                        : TrafficControl.StartParallelLight(deviceId);
            try
            {
                if (!lockAcquired)
                {
                    error = "Lock timeout";
                    return new PingElement { Status = "Locked" };
                }
                // LLAMAMOS AL INTERNO
                return ReadInternal(ip, out error, timeout, attempts);
            }
            finally
            {
                if (lockAcquired)
                {
                    Thread.Sleep(_delayms);
                    if (isHeavy) TrafficControl.StopExclusiveHeavy(deviceId);
                    else TrafficControl.StopParallelLight(deviceId);
                }
            }
        }
        private PingElement ReadInternal(string ip, out string error, int timeout = 300, int attempts = 10)
        {
            error = "";
            PingElement result = new PingElement { IP = ip, Absent = true };
            var latencies = new List<long>();
            int successfulPings = 0;

            try
            {
                using (Ping myPing = new Ping())
                {
                    for (int i = 0; i < attempts; i++)
                    {
                        try
                        {
                            PingReply reply = myPing.Send(ip, timeout);
                            if (reply != null && reply.Status == IPStatus.Success)
                            {
                                successfulPings++;
                                latencies.Add(reply.RoundtripTime);
                            }
                        }
                        catch { /* Error de socket individual */ }

                        // Respiro entre pings de la misma r�faga
                        if (i < attempts - 1) Thread.Sleep(10);
                    }
                }

                // --- Procesamiento Estad�stico ---
                result.SuccessCount = successfulPings;
                result.LossRate = ((double)(attempts - successfulPings) / attempts) * 100;

                if (successfulPings > 0)
                {
                    result.LastSeen = DateTime.UtcNow;
                    result.Absent = false;
                    result.AverageTOF = (long)(latencies.Count > 0 ? latencies.Average() : 0);

                    // Clasificaci�n de precisi�n (Pasos de 10% en 10%)
                    if (result.LossRate == 0) result.Status = "Perfect";
                    else if (result.LossRate <= 20) result.Status = "Stable (Minor Loss)";
                    else if (result.LossRate <= 50) result.Status = "Unstable";
                    else result.Status = "Degraded";
                }
                else
                {
                    result.Status = "Down";
                }
            }
            catch (Exception ex)
            {
                error = $"[ERROR(ICMP)] {ip}: {ex.Message}";
                result.Status = "Error";
            }
            finally
            {
                Thread.Sleep(_delayms);
            }
            return result;
        }
        private void DiscoverBestIP(string deviceId, List<string> ips)
        {
            var tasks = new List<Task<PingElement>>();

            // BLOQUEO NIVEL SUPERIOR: Una sola vez para todas las IPs
            TrafficControl.StartParallelLight(deviceId);
            try
            {
                foreach (string ip in ips)
                {
                    string currentIp = ip;
                    tasks.Add(Task.Factory.StartNew(() => {
                        string err;
                        // LLAMAMOS AL INTERNO: As� evitamos llamar a TrafficControl de nuevo
                        return this.ReadInternal(currentIp, out err, 300, 3);
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                var results = tasks.Select(t => t.Result).ToList();
                var best = results.Where(r => !r.Absent).OrderBy(r => r.LossRate).ThenBy(r => r.AverageTOF).FirstOrDefault();
                if (best != null)
                    RouteRegistry.Update(deviceId, best.IP);
                else
                    RouteRegistry.Remove(deviceId);
            }
            finally
            {
                TrafficControl.StopParallelLight(deviceId);
            }
        }
        public string GetIp(string deviceId, List<string> ips, out string error)
        {
            error = string.Empty;
            string targetIp = RouteRegistry.Get(deviceId);
            if (string.IsNullOrEmpty(targetIp))
            {
                this.DiscoverBestIP(deviceId, ips);
                targetIp = RouteRegistry.Get(deviceId);
                if (string.IsNullOrEmpty(targetIp))
                {
                    error = $"Device {deviceId} ips didn't respond";
                    return string.Empty;
                }    
                return targetIp;
            }
            var quickCheck = this.ReadInternal(targetIp, out error, timeout: 200, attempts: 1);
            if (quickCheck.Absent)
            {
                this.DiscoverBestIP(deviceId, ips);
                targetIp = RouteRegistry.Get(deviceId);
                if (string.IsNullOrEmpty(targetIp))
                {
                    error = $"Device {deviceId} ips didn't respond";
                    return string.Empty;
                }
                return targetIp;
            }
            return targetIp;
        }
    }
}


