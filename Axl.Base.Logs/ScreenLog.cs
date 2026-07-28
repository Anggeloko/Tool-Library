﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Logs
{
    public class ScreenLog : ILog
    {
        private readonly IJson _serialize;
        private readonly EnvironmentType _env;
        private static readonly object _wr = new object();

        public ScreenLog(IJson serializer, string env = "production")
        {
            _serialize = serializer;
            _env = EnvironmentParser.Parse(env);
        }

        private void Write(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            message = Formats.Path(message);
            lock (_wr)
            {
                Console.WriteLine($"[{DateTime.Now.ToLongTime()}] {message}");
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
            Console.Clear();
        }
    }
}

