using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Axl.Base.Models;
using Axl.Base.Statics;
using System.Data.SQLite;

namespace Axl.Base.Database.SQLite.Services
{

    public class SQLiteService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        static SQLiteService()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // Si estamos en un test o entorno especial, Location puede ser más fiable
                var assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                
                var is64Bit = IntPtr.Size == 8;
                var subDir = is64Bit ? "x64" : "x86";
                
                // Intentamos en ambos directorios (base y el del assembly)
                string[] possibleDirs = { baseDir, assemblyDir };
                
                foreach (var dir in possibleDirs)
                {
                    if (string.IsNullOrEmpty(dir)) continue;
                    
                    var targetDir = Path.Combine(dir, subDir);
                    var targetFile = Path.Combine(targetDir, "SQLite.Interop.dll");

                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    
                    if (!File.Exists(targetFile))
                    {
                        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                        // El nombre del recurso en SDK-style suele ser [AssemblyName].Resources.[subDir].SQLite.Interop.dll
                        var resourceName = $"Axl.Base.Database.SQLite.Resources.{subDir}.SQLite.Interop.dll";
                        
                        using (var stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream != null)
                            {
                                using (var fileStream = File.Create(targetFile))
                                {
                                    stream.CopyTo(fileStream);
                                }
                            }
                        }
                    }

                    if (File.Exists(targetFile))
                    {
                        LoadLibrary(targetFile);
                    }
                }
            }
            catch { /* Silencioso para no romper el estático */ }
        }

        public SQLiteService(string dataPath)
        {
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            _dbPath = Path.Combine(dataPath, "settings.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";
            InitializeDatabase();
        }
        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Variables (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                    )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
        public string GetVariable(string key, string defaultValue = null)
        {
            if (!File.Exists(_dbPath)) return defaultValue;

            var keyPool = MachineInfo.GetPossibleHashes(16);
            var primaryKey = keyPool.FirstOrDefault();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                foreach (var hash in keyPool)
                {
                    var encryptedKey = Encryption.Encrypt(key, hash);
                    var selectQuery = "SELECT Value FROM Variables WHERE Key = @Key";
                    using (var command = new SQLiteCommand(selectQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Key", encryptedKey);
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            var encryptedValue = result.ToString();
                            var decryptedValue = Encryption.Decrypt(encryptedValue, hash);
                            if (decryptedValue != "[Decryption Error]")
                            {
                                if (hash != primaryKey)
                                {
                                    SetVariable(key, decryptedValue);
                                }
                                return decryptedValue;
                            }
                        }
                    }
                }
            }
            return defaultValue;
        }

        public void SetVariable(string key, string value)
        {
            var keyPool = MachineInfo.GetPossibleHashes(16);
            var primaryKey = keyPool.FirstOrDefault();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var hash in keyPool)
                    {
                        var encryptedKey = Encryption.Encrypt(key, hash);
                        var deleteQuery = "DELETE FROM Variables WHERE Key = @Key";
                        using (var deleteCmd = new SQLiteCommand(deleteQuery, connection))
                        {
                            deleteCmd.Parameters.AddWithValue("@Key", encryptedKey);
                            deleteCmd.ExecuteNonQuery();
                        }
                    }

                    var insertQuery = "INSERT INTO Variables (Key, Value) VALUES (@Key, @Value)";
                    using (var insertCmd = new SQLiteCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@Key", Encryption.Encrypt(key ?? "", primaryKey));
                        insertCmd.Parameters.AddWithValue("@Value", Encryption.Encrypt(value ?? "", primaryKey));
                        insertCmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        public void DeleteVariable(string key)
        {
            var keyPool = MachineInfo.GetPossibleHashes(16);
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var hash in keyPool)
                    {
                        var encryptedKey = Encryption.Encrypt(key, hash);
                        var deleteQuery = "DELETE FROM Variables WHERE Key = @Key";
                        using (var deleteCmd = new SQLiteCommand(deleteQuery, connection))
                        {
                            deleteCmd.Parameters.AddWithValue("@Key", encryptedKey);
                            deleteCmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        public void SaveVariables(IEnumerable<VariableItem> variables)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var deleteQuery = "DELETE FROM Variables";
                    using (var deleteCommand = new SQLiteCommand(deleteQuery, connection))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }

                    var insertQuery = "INSERT INTO Variables (Key, Value) VALUES (@Key, @Value)";
                    using (var insertCommand = new SQLiteCommand(insertQuery, connection))
                    {
                        var pKey = new SQLiteParameter("@Key");
                        var pVal = new SQLiteParameter("@Value");
                        insertCommand.Parameters.Add(pKey);
                        insertCommand.Parameters.Add(pVal);

                        // Minimize key exposure scope
                        var key = MachineInfo.Hash(16);

                        foreach (var variable in variables)
                        {
                            // Encrypt both key and value for total obfuscation
                            pKey.Value = Encryption.Encrypt(variable.Key ?? "", key);
                            pVal.Value = Encryption.Encrypt(variable.Value ?? "", key);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        public List<VariableItem> ReadVariables()
        {
            var variables = new List<VariableItem>();
            if (!File.Exists(_dbPath)) return variables;

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Obtenemos todas las posibles llaves de esta máquina
                var keyPool = MachineInfo.GetPossibleHashes(16);
                var primaryKey = keyPool.FirstOrDefault();
                bool healingRequired = false;

                var selectQuery = "SELECT Key, Value FROM Variables";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var encKey = reader.GetString(0);
                            var encVal = reader.GetString(1);

                            // Intentamos descubrir qué llave funciona para este registro
                            string decryptedKey = "[Decryption Error]";
                            string currentWorkingKey = null;

                            foreach (var key in keyPool)
                            {
                                var tempKey = Encryption.Decrypt(encKey, key);
                                if (tempKey != "[Decryption Error]")
                                {
                                    decryptedKey = tempKey;
                                    currentWorkingKey = key;
                                    break;
                                }
                            }

                            if (decryptedKey != "[Decryption Error]")
                            {
                                var decryptedValue = Encryption.Decrypt(encVal, currentWorkingKey);
                                variables.Add(new VariableItem { Key = decryptedKey, Value = decryptedValue });

                                // Si la llave que funcionó NO es la primaria, necesitamos "curar" la base de datos
                                if (currentWorkingKey != primaryKey) healingRequired = true;
                            }
                        }
                    }
                }

                // AUTO-CURACIÓN: Si se usó una llave vieja o secundaria, re-encriptamos todo con la llave estable primaria
                if (healingRequired && variables.Count > 0)
                {
                    SaveVariables(variables);
                }
            }
            return variables;
        }
    }
}


