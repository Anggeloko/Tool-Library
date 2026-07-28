﻿using System;

namespace Axl.Base.Models
{
    public class MqttData : EventArgs
    {
        public string Topic { get; set; }
        public string Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

