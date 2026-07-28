using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Axl.Base.Interfaces
{
    /// <summary>
    /// Unified logging interface supporting multiple severity levels and dynamic object tracing.
    /// </summary>
    public interface ILog
    {
        /// <summary>Logs an error message.</summary>
        void Error(string message, [CallerMemberName] string method = "");
        
        /// <summary>Logs a collection of error messages (e.g., from an Exception trace).</summary>
        void Error(List<string> lst, [CallerMemberName] string method = "");
        
        /// <summary>Logs a warning message.</summary>
        void Warn(string message, [CallerMemberName] string method = "");
        
        /// <summary>Logs a fatal error message that typically precedes application termination.</summary>
        void Fatal(string message, [CallerMemberName] string method = "");
        
        /// <summary>Logs high-verbosity trace information for debugging deep logic flows.</summary>
        void Trace(string message, [CallerMemberName] string method = "");
        
        /// <summary>Logs an informational message.</summary>
        void Info(string message, [CallerMemberName] string method = "");
        
        /// <summary>Logs a complex object as a JSON-serialized string (useful for debugging payloads).</summary>
        void Info(dynamic obj, [CallerMemberName] string method = "");
        
        /// <summary>Logs a debug-level message.</summary>
        void Debug(string message, [CallerMemberName] string method = "");
        
        /// <summary>Performs maintenance tasks (e.g., cleaning old log files).</summary>
        void Support();
    }
}
