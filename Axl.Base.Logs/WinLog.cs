﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Logs
{
    public class WinLog : ILog
    {
        private readonly string _app;
        private readonly IJson _serialize;
        private readonly EnvironmentType _env;
        private static readonly object _wr = new object();

        public WinLog(IJson serializer, string source, string env = "production")
        {
            _serialize = serializer;
            _app = source;
            _env = EnvironmentParser.Parse(env);
        }

        private void Write(string message, EventLogEntryType level)
        {
            if (string.IsNullOrEmpty(message)) return;
            message = Formats.Path(message);
            lock (_wr)
            {
                EventLog.WriteEntry(_app, message, level);
            }
        }

        public void Error(List<string> lst, [CallerMemberName] string method = "")
        {
            if (!Enum.IsDefined(typeof(EnvironmentType), _env)) return;
            int i = 0;
            foreach (string s in lst)
            {
                Write(i == 0 ? $"[{method}(ERROR)] {s}" : $"\t[{method}(ERROR)] {s}", EventLogEntryType.Error);
                i++;
            }
        }

        public void Error(string msg, [CallerMemberName] string method = "") => Write(Enum.IsDefined(typeof(EnvironmentType), _env) ? $"[{method}(ERROR)] {msg}" : string.Empty, EventLogEntryType.Error);
        public void Warn(string msg, [CallerMemberName] string method = "") => Write(Enum.IsDefined(typeof(EnvironmentType), _env) ? $"[{method}(WARNING)] {msg}" : string.Empty, EventLogEntryType.Warning);
        public void Fatal(string msg, [CallerMemberName] string method = "") => Write(Enum.IsDefined(typeof(EnvironmentType), _env) ? $"[{method}(FATAL)] {msg}" : string.Empty, EventLogEntryType.Error);
        public void Trace(string msg, [CallerMemberName] string method = "") => Write(_env == EnvironmentType.Debug ? $"[{method.ToUpper()}(TRACE)] {msg}" : string.Empty, EventLogEntryType.Information);
        public void Info(string msg, [CallerMemberName] string method = "") => Write(Enum.IsDefined(typeof(EnvironmentType), _env) ? $"[{method}(INFO)] {msg}" : string.Empty, EventLogEntryType.Information);
        public void Info(dynamic obj, [CallerMemberName] string method = "") => Write(_env == EnvironmentType.Debug ? $"[{method.ToUpper()}](JSON) {_serialize.Serialize(obj)}" : string.Empty, EventLogEntryType.Information);
        public void Debug(string msg, [CallerMemberName] string method = "") => Write((_env == EnvironmentType.Development || _env == EnvironmentType.Debug) ? $"[{method.ToUpper()}(DEBUG)] {msg}" : string.Empty, EventLogEntryType.Information);

        public void Support() { }
    }
}

