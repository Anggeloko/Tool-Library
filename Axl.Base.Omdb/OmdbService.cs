﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Axl.Base.Statics;

namespace Axl.Base.Omdb
{
    /// <summary>
    /// Service to interact with the Foxboro OM Database through shell commands (ksh.exe).
    /// </summary>
    public class OmdbService
    {
        public const string DefaultFoxboroPath = @"d:\opt\fox\bin\tools";
        private readonly string _workingDirectory;
        private readonly string _shellName = "ksh.exe";

        /// <summary>
        /// Initializes a new instance of the OmdbService.
        /// </summary>
        /// <param name="workingDirectory">Path to the Foxboro Axl.Base. Defaults to d:\opt\fox\bin\tools</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if the path does not exist.</exception>
        public OmdbService(string workingDirectory = DefaultFoxboroPath)
        {
            if (!Directory.Exists(workingDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"OMDB Critical Error: Foxboro tools directory not found at '{workingDirectory}'. " +
                    "Please verify the installation path or provide a valid one.");
            }

            _workingDirectory = workingDirectory;
        }

        #region Create Methods

        /// <summary>
        /// Bulk creates variables in the OM database.
        /// </summary>
        public void CreateVariables(IEnumerable<string> variables, OmType type)
        {
            foreach (var variable in variables)
            {
                EnsureCreated(variable, type);
            }
        }

        /// <summary>
        /// Creates a sequence of variables (e.g., VAR_001, VAR_002) in the OM database.
        /// </summary>
        public void CreateRawVariables(string prefix, int quantity, OmType type)
        {
            if (quantity <= 0 || string.IsNullOrEmpty(prefix)) return;
            
            for (int i = 1; i <= quantity; i++)
            {
                string varName = $"{prefix}_{i:D3}";
                EnsureCreated(varName, type);
            }
        }

        #endregion

        #region Write Methods

        /// <summary>
        /// Writes a value to a variable in the OM database.
        /// </summary>
        public void Write(string variable, object value, OmType type)
        {
            EnsureCreated(variable, type);
            string flag = GetFlagForType(type);
            string formattedValue = FormatValue(value, type);
            ExecuteCommand($"omsetimp {variable} {flag} {formattedValue}", isWrite: true);
        }

        #endregion

        #region Read Methods

        /// <summary>
        /// Reads a value from the OM database.
        /// </summary>
        public T Read<T>(string variable, out bool exists)
        {
            exists = false;
            string output = ExecuteCommand($"omgetimp {variable}", true);

            if (string.IsNullOrEmpty(output) || output.Contains("not"))
                return default(T);

            exists = true;
            return ParseOutput<T>(output);
        }

        /// <summary>
        /// Reads a specific bit from a packed long variable.
        /// </summary>
        public bool ReadBit(string variable, int bit, out bool exists)
        {
            exists = false;
            string output = ExecuteCommand($"omgetimp {variable}", true);

            if (string.IsNullOrEmpty(output) || output.Contains("not") || !output.Contains("(pl)"))
                return false;

            try
            {
                // Logic based on original: Extract hex after 0x
                string hexPart = output.Substring(output.IndexOf("0x") + 2).Trim().ToLower();
                // Ensure it has 8 chars for a 32-bit hex
                if (hexPart.Length < 8) hexPart = hexPart.PadLeft(8, '0');

                // Original logic: bit / 4 to get the char index from the end
                int charIndex = 7 - (bit / 4);
                if (charIndex < 0 || charIndex >= hexPart.Length) return false;

                int hexVal = CharToHex(hexPart[charIndex]);
                int bitInNibble = bit % 4;
                
                exists = true;
                return (hexVal & (1 << bitInNibble)) != 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Execution Core

        private string ExecuteCommand(string command, bool redirectOutput = false, bool isWrite = false)
        {
            string lockId = "OMDB_GLOBAL"; // Global lock for the database access
            
            // Acquire lock: Exclusive for writes, Parallel for reads
            bool lockAcquired = isWrite ? 
                TrafficControl.StartExclusiveHeavy(lockId) : 
                TrafficControl.StartParallelLight(lockId);

            if (!lockAcquired)
                throw new TimeoutException($"OMDB Error: Timeout waiting for database access lock during command: {command}");

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = _shellName,
                        WorkingDirectory = _workingDirectory,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = redirectOutput,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    process.Start();

                    using (StreamWriter sw = process.StandardInput)
                    {
                        sw.WriteLine(command);
                    }

                    string result = redirectOutput ? process.StandardOutput.ReadToEnd() : string.Empty;
                    process.WaitForExit();
                    
                    return result.Trim();
                }
            }
            finally
            {
                // Release lock
                if (isWrite) TrafficControl.StopExclusiveHeavy(lockId);
                else TrafficControl.StopParallelLight(lockId);
            }
        }

        private void EnsureCreated(string variable, OmType type)
        {
            // Check if exists (Read mode)
            string check = ExecuteCommand($"omfnd {variable}", true, isWrite: false);
            if (check.Contains("NOT"))
            {
                // Create (Write mode)
                string flag = GetFlagForType(type);
                ExecuteCommand($"omcrt {flag} {variable}", isWrite: true);
            }
        }

        #endregion

        #region Mapping Helpers

        private string GetFlagForType(OmType type)
        {
            switch (type)
            {
                case OmType.Bool: return "-b";
                case OmType.String: return "-v -s";
                case OmType.Float: return "-f";
                case OmType.Int: return "-i";
                case OmType.Long: return "-l";
                default: return "";
            }
        }

        private string FormatValue(object value, OmType type)
        {
            if (type == OmType.Bool) return (bool)value ? "TRUE" : "FALSE";
            if (type == OmType.String) return $"'{value}'";
            return value.ToString();
        }

        private T ParseOutput<T>(string output)
        {
            try
            {
                // Boolean Logic
                if (typeof(T) == typeof(bool))
                    return (T)(object)output.Contains("TRUE");

                // String Logic
                if (typeof(T) == typeof(string))
                {
                    int start = output.IndexOf("[");
                    int end = output.LastIndexOf("]");
                    if (start != -1 && end != -1)
                        return (T)(object)output.Substring(start + 1, end - start - 1);
                    return (T)(object)string.Empty;
                }

                // Numeric Logic
                if (typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(int) || typeof(T) == typeof(long))
                {
                    // Find the value after the type indicator (e.g., (f), (i), (l))
                    int lastClosingParen = output.LastIndexOf(")");
                    if (lastClosingParen != -1)
                    {
                        string s = output.Substring(lastClosingParen + 1).Trim();
                        return (T)Convert.ChangeType(s, typeof(T));
                    }
                }
            }
            catch { }
            return default(T);
        }

        private int CharToHex(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return 0;
        }

        #endregion
    }

    /// <summary>
    /// Supported OM Database variable types.
    /// </summary>
    public enum OmType
    {
        Bool,
        String,
        Float,
        Int,
        Long
    }
}

