﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axl.Base.Models
{
    public class PingElement
    {
        public string IP { get; set; } = "0.0.0.0";

        // Control de presencia
        public bool Absent { get; set; } = true;

        // M�tricas de red
        public double LossRate { get; set; } = 100;
        public long AverageTOF { get; set; } = 0;
        public int SuccessCount { get; set; } = 0;

        // Estado descriptivo (Perfect, Stable, Locked, Down, etc.)
        public string Status { get; set; } = "Unknown";

        // Timestamp para saber cu�ndo fue la �ltima vez que lo vimos
        public DateTime? LastSeen { get; set; } = null;
    }
}

