﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using Axl.Base.Models;

namespace Axl.Base.Statics
{
    public static class MachineInfo
    {
        public static string Hash(int len = 16)
        {
            try
            {
                return GetPossibleHashes(len).FirstOrDefault();
            }
            catch (Exception)
            {
                return Environment.MachineName.GetHashCode().ToString("X");
            }
        }

        public static List<string> GetPossibleHashes(int len)
        {
            var hashes = new List<string>();
            try
            {
                // Datos de Identidad Ultra-Estables (Hardware Fijo)
                var uuid = (SystemUuid() ?? "UUNA").Trim();
                var biosSerial = (BiosSerial() ?? "BSNA").Trim();
                var boardId = (BaseBoardId() ?? "MBNA").Trim();

                // 1. IDENTIDAD PRINCIPAL: Hardware Puro (Inmune a cambios de nombre de PC o Red)
                var rawHardware = string.Format("HW|{0}|{1}|{2}", uuid, biosSerial, boardId);
                hashes.Add(GenerateHash(rawHardware, len));

                // 2. FALLBACK: Hardware + Nombre (Por compatibilidad si se usó anteriormente)
                var machineName = Name().ToUpper();
                var rawWithMachine = string.Format("{0}|{1}|{2}|{3}", machineName, uuid, biosSerial, boardId);
                hashes.Add(GenerateHash(rawWithMachine, len));

                // Fallback de emergencia (Legacy/Name-based)
                hashes.Add(Environment.MachineName.GetHashCode().ToString("X"));
            }
            catch { }

            return hashes.Distinct().ToList();
        }

        private static string GenerateHash(string rawData, int len)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                var hexHash = BitConverter.ToString(hashBytes).Replace("-", "");
                return hexHash.Length > len ? hexHash.Substring(0, len) : hexHash;
            }
        }

        public static string Name()
        {
            try { return Environment.MachineName; }
            catch { return "UMNA"; }
        }

        public static List<DriveData> Drives()
        {
            var drives = new List<DriveData>();
            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    drives.Add(new DriveData
                    {
                        Name = drive.Name,
                        VolumeLabel = drive.VolumeLabel,
                        DriveFormat = drive.DriveFormat,
                        TotalSize = drive.TotalSize,
                        AvailableFreeSpace = drive.AvailableFreeSpace,
                        PercentageUsed = drive.TotalSize > 0 ? Math.Round(((double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize) * 100, 2) : 0
                    });
                }
            }
            catch { }
            return drives;
        }

        public static RamData Ram()
        {
            var ramData = new RamData();
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var totalRamKb = (ulong)obj["TotalVisibleMemorySize"];
                    var freeRamKb = (ulong)obj["FreePhysicalMemory"];
                    ramData.TotalPhysicalMemory = totalRamKb * 1024;
                    ramData.AvailablePhysicalMemory = freeRamKb * 1024;
                    if (totalRamKb > 0) ramData.PercentageUsed = Math.Round(((double)(totalRamKb - freeRamKb) / totalRamKb) * 100, 2);
                }
            }
            catch { }
            return ramData;
        }

        public static List<ServiceData> Services()
        {
            var services = new List<ServiceData>();
            try
            {
                foreach (var service in ServiceController.GetServices())
                {
                    services.Add(new ServiceData { ServiceName = service.ServiceName, DisplayName = service.DisplayName, Status = service.Status.ToString() });
                }
            }
            catch { }
            return services;
        }

        public static CpuData Cpu()
        {
            var cpuData = new CpuData();
            try
            {
                using (var searcherName = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcherName.Get())
                    {
                        cpuData.ProcessorName = obj["Name"]?.ToString().Trim();
                        cpuData.NumberOfCores = Convert.ToInt32(obj["NumberOfCores"]);
                        cpuData.NumberOfLogicalProcessors = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                    }
                }
                using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    cpuCounter.NextValue();
                    System.Threading.Thread.Sleep(100);
                    cpuData.PercentageUsed = Math.Round(cpuCounter.NextValue(), 2);
                }
            }
            catch { }
            return cpuData;
        }

        public static OsData Os()
        {
            var osData = new OsData();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, OSArchitecture, LastBootUpTime FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        osData.OSName = obj["Caption"]?.ToString();
                        osData.Architecture = obj["OSArchitecture"]?.ToString();
                        var lastBootUpTime = obj["LastBootUpTime"]?.ToString();
                        if (!string.IsNullOrEmpty(lastBootUpTime))
                        {
                            osData.LastBootUpTime = ManagementDateTimeConverter.ToDateTime(lastBootUpTime);
                            osData.Uptime = DateTime.Now - osData.LastBootUpTime;
                        }
                    }
                }
            }
            catch { }
            return osData;
        }

        public static List<NetworkInterfaceData> Network()
        {
            var interfaces = new List<NetworkInterfaceData>();
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up))
                {
                    var data = new NetworkInterfaceData { Name = ni.Name, Description = ni.Description, MacAddress = ni.GetPhysicalAddress().ToString(), IPAddresses = new List<string>() };
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) data.IPAddresses.Add(ip.Address.ToString());
                    }
                    if (data.IPAddresses.Count > 0) interfaces.Add(data);
                }
            }
            catch { }
            return interfaces;
        }

        private static string BaseBoardId()
        {
            try
            {
                string boardSerial = "";
                using (var searcher = new ManagementObjectSearcher("Select SerialNumber From Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        boardSerial = obj["SerialNumber"]?.ToString();
                        if (!string.IsNullOrEmpty(boardSerial)) break;
                    }
                }
                return string.IsNullOrWhiteSpace(boardSerial) || boardSerial.ToLower().Contains("fill") ? "MBNA" : boardSerial.Trim();
            }
            catch { return "MBNA"; }
        }

        private static string SystemUuid()
        {
            try
            {
                string uuid = "";
                using (var searcher = new ManagementObjectSearcher("Select UUID From Win32_ComputerSystemProduct"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        uuid = obj["UUID"]?.ToString();
                        if (!string.IsNullOrEmpty(uuid)) break;
                    }
                }
                return string.IsNullOrWhiteSpace(uuid) || uuid.Contains("FFFF") ? "UUNA" : uuid.Trim();
            }
            catch { return "UUNA"; }
        }

        private static string BiosSerial()
        {
            try
            {
                string serial = "";
                using (var searcher = new ManagementObjectSearcher("Select SerialNumber From Win32_BIOS"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        serial = obj["SerialNumber"]?.ToString();
                        if (!string.IsNullOrEmpty(serial)) break;
                    }
                }
                return string.IsNullOrWhiteSpace(serial) || serial.ToLower().Contains("fill") ? "BSNA" : serial.Trim();
            }
            catch { return "BSNA"; }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern bool GetVolumeInformation(string rootPathName, StringBuilder volumeNameBuffer, int volumeNameSize, out uint volumeSerialNumber, out uint maximumComponentLength, out uint fileSystemFlags, StringBuilder fileSystemNameBuffer, int nFileSystemNameSize);
        private static string VolumeSerial(string drive)
        {
            uint serialNumber, maxComponentLength, fileSystemFlags;
            if (GetVolumeInformation(drive, null, 0, out serialNumber, out maxComponentLength, out fileSystemFlags, null, 0)) return serialNumber.ToString("X");
            return "00000000";
        }
    }
}

