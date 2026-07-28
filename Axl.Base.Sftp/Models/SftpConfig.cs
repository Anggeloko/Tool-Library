﻿using System;

namespace Axl.Base.Sftp.Models
{
    public class SftpConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 22;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RemotePath { get; set; } = string.Empty;
        public string LocalPath { get; set; } = string.Empty;
    }
}

