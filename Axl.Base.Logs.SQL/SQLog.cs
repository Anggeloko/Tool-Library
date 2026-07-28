﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Logs.SQL
{
    public class SQLog : ILog
    {
        private readonly string _builder;
        private readonly int _limit;
        private readonly IJson _serialize;
        private readonly ISql _sql;
        private readonly EnvironmentType _env;
        private readonly string _tlog;
        private static readonly object _processLock = new object();
        private static readonly object _writeLock = new object();

        public SQLog(IJson serializer, ISql sql, string builder = "mssql", string logtable = "Log", string env = "production", int limit = 7)
        {
            _serialize = serializer;
            _env = EnvironmentParser.Parse(env);
            _limit = limit;
            _sql = sql;
            _tlog = logtable;
            
            StringBuilder sqlBuilder = new StringBuilder();
            if (builder.ToLower().Contains("my"))
            {
                sqlBuilder.Append($"CREATE TABLE IF NOT EXISTS `{_tlog}` ( ");
                sqlBuilder.Append("    `LogId` INT PRIMARY KEY AUTO_INCREMENT, ");
                sqlBuilder.Append("    `LogDate` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3), ");
                sqlBuilder.Append("    `Message` TEXT NOT NULL ");
                sqlBuilder.Append(") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
                _builder = sqlBuilder.ToString();
            }
            else if (builder.ToLower().Contains("ms"))
            {
                sqlBuilder.Append($"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{_tlog}') AND type in (N'U')) ");
                sqlBuilder.Append("BEGIN ");
                sqlBuilder.Append($"    CREATE TABLE {_tlog} ( ");
                sqlBuilder.Append("        [LogId] INT PRIMARY KEY IDENTITY(1,1), ");
                sqlBuilder.Append("        [LogDate] DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(), ");
                sqlBuilder.Append("        [Message] NVARCHAR(MAX) NOT NULL ");
                sqlBuilder.Append("    ); ");
                sqlBuilder.Append("END ");
                _builder = sqlBuilder.ToString();
            }
            else throw new Exception("Database engine isn't supported");

            Create();
        }

        private void Create()
        {
            _sql.Execute(_builder).Wait();
            Support();
        }

        private void Write(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            message = Formats.Path(message);
            lock (_writeLock)
            {
                try
                {
                    _sql.Execute($"INSERT INTO {_tlog} (LogDate, Message) VALUES (@LogDate, @Message)", new { LogDate = DateTime.Now.ToFullDateTime(), Message = message }).Wait();
                }
                catch { }
            }
        }

        public void Error(List<string> messages, [CallerMemberName] string method = "")
        {
            if (!Enum.IsDefined(typeof(EnvironmentType), _env)) return;
            int index = 0;
            foreach (string message in messages)
            {
                Write(index == 0 ? $"[{method}(ERROR)] {message}" : $"\t[{method}(ERROR)] {message}");
                index++;
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
            lock (_processLock)
            {
                try
                {
                    DateTime limitDate = DateTime.Now.AddDays(-_limit);
                    _sql.Execute($"DELETE FROM {_tlog} WHERE LogDate <= @LogDate", new { LogDate = limitDate.ToFullDateTime() });
                }
                catch { }
            }
        }
    }
}

