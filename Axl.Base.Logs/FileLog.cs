﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Logs
{
    public class FileLog : ILog
    {
        private readonly string _path;
        private readonly int _limit;
        private readonly IJson _serialize;
        private readonly EnvironmentType _env;
        private static readonly object _pr = new object();
        private static readonly object _wr = new object();

        public FileLog(IJson serializer, string env = "production", string logPath = "", int logRetentionLimit = 7)
        {
            _serialize = serializer;
            _path = logPath.Length > 0 ? logPath.EndsWith(@"\") ? logPath : $@"{logPath}\" : $@"{AppDomain.CurrentDomain.BaseDirectory}Log\";
            _limit = logRetentionLimit;
            _env = EnvironmentParser.Parse(env);
            Create();
        }

        private void Create()
        {
            Verify();
            Support();
        }

        private void Verify()
        {
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
                Write("Logs folder created");
            }
        }

        private void Write(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            message = Formats.Path(message);
            lock (_wr)
            {
                try
                {
                    File.AppendAllText(Path.Combine(_path, $"{DateTime.Now.ToCompactDate()}.log"), $"[{DateTime.Now.ToLongTime()}] {message}{Environment.NewLine}");
                }
                catch (DirectoryNotFoundException)
                {
                    Directory.CreateDirectory(_path);
                    File.AppendAllText(Path.Combine(_path, $"{DateTime.Now.ToCompactDate()}.log"), $"[{DateTime.Now.ToLongTime()}] {message}{Environment.NewLine}");
                }
            }
        }

        public void Error(List<string> lst, [CallerMemberName] string method = "")
        {
            if (!Enum.IsDefined(typeof(EnvironmentType), _env)) return;
            int i = 0;
            foreach (string s in lst)
            {
                Write(i == 0 ? $"[{method}(ERROR)] {s}" : $"\t[{method}(ERROR)] {s}");
                i++;
            }
        }

        public void Error(string msg, [CallerMemberName] string method = "") => Write(Enum.IsDefined(typeof(EnvironmentType), _env) ? $"[{method}(ERROR)] {msg}" : string.Empty);
        public void Warn(string msg, [CallerMemberName] string method = "") => Write(Enum.IsDefined(typeof(EnvironmentType), _env) ? $"[{method}(WARNING)] {msg}" : string.Empty);
        public void Fatal(string msg, [CallerMemberName] string method = "") => Write(Enum.IsDefined(typeof(EnvironmentType), _env) ? $"[{method}(FATAL)] {msg}" : string.Empty);
        public void Trace(string msg, [CallerMemberName] string method = "") => Write(_env == EnvironmentType.Debug ? $"[{method.ToUpper()}(TRACE)] {msg}" : string.Empty);
        public void Info(string msg, [CallerMemberName] string method = "") => Write(Enum.IsDefined(typeof(EnvironmentType), _env) ? $"[{method}(INFO)] {msg}" : string.Empty);
        public void Info(dynamic obj, [CallerMemberName] string method = "") => Write(_env == EnvironmentType.Debug ? $"[{method.ToUpper()}](JSON) {_serialize.Serialize(obj)}" : string.Empty);
        public void Debug(string msg, [CallerMemberName] string method = "") => Write((_env == EnvironmentType.Development || _env == EnvironmentType.Debug) ? $"[{method.ToUpper()}(DEBUG)] {msg}" : string.Empty);

        public void Support()
        {
            lock (_pr)
            {
                try
                {
                    if (!Directory.Exists(_path)) return;
                    var logFiles = Directory.GetFiles(_path, "*.log", SearchOption.TopDirectoryOnly);
                    DateTime dtlimit = DateTime.Now.AddDays(-_limit);
                    foreach (var logFile in logFiles)
                    {
                        var fileInfo = new FileInfo(logFile);
                        if (fileInfo.CreationTime < dtlimit) fileInfo.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Write($"Error cleaning up logs: {ex.Message}");
                }
            }
        }
    }
}

