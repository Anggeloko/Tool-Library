﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axl.Base.Models
{
    public class DriveData
    {
        public string Name { get; set; }
        public string VolumeLabel { get; set; }
        public string DriveFormat { get; set; }
        public long TotalSize { get; set; }
        public long AvailableFreeSpace { get; set; }
        public double PercentageUsed { get; set; }
    }

    public class RamData
    {
        public ulong TotalPhysicalMemory { get; set; }
        public ulong AvailablePhysicalMemory { get; set; }
        public double PercentageUsed { get; set; }
    }

    public class ServiceData
    {
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
    }

    public class CpuData
    {
        public string ProcessorName { get; set; }
        public int NumberOfCores { get; set; }
        public int NumberOfLogicalProcessors { get; set; }
        public double PercentageUsed { get; set; }
    }

    public class OsData
    {
        public string OSName { get; set; }
        public string Architecture { get; set; }
        public DateTime LastBootUpTime { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    public class NetworkInterfaceData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string MacAddress { get; set; }
        public List<string> IPAddresses { get; set; }
    }

    public class SecurityPatchData
    {
        public string HotFixID { get; set; }
        public string Description { get; set; }
        public string InstalledOn { get; set; }
    }

    public class ProcessData
    {
        public string ProcessName { get; set; }
        public int Id { get; set; }
        public double MemoryUsedMB { get; set; }
    }

    public class CurrentProcessMemoryData
    {
        public string ProcessName { get; set; }
        public double MemoryUsedMB { get; set; }
    }
}

