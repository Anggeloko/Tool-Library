using Axl.Base.Interfaces;
using Axl.Base.Statics;
using Axl.Base.Persistence.Interfaces;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Axl.Base.Persistence.Services
{
    /// <summary>
    /// Implementation of variable storage using local files with encryption and compression.
    /// </summary>
    public class LocalVariableStorage : IVariableStorage
    {
        private readonly string _basePath;
        private readonly IJson _json;
        private readonly ILog _logger;
        private readonly int _cleanupDays;
        private const string Ext = ".bin";

        public bool UseCompression { get; set; } = true;

        public LocalVariableStorage(string basePath, IJson jsonProvider, ILog logger, int cleanupDays = 1)
        {
            _basePath = basePath;
            _json = jsonProvider;
            _logger = logger;
            _cleanupDays = cleanupDays;
        }

        public T Load<T>(string key, string subDir = "Data") where T : new()
        {
            try
            {
                if (string.IsNullOrEmpty(_basePath)) return default(T);

                string dataPath = Path.Combine(_basePath, subDir);
                if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
                
                if (_cleanupDays > 0)
                {
                    WindowsHelper.FileCleaner(dataPath, _cleanupDays);
                }

                string file = Path.Combine(dataPath, key + Ext);
                string val = string.Empty;

                if (File.Exists(file))
                {
                    try
                    {
                        byte[] fileBytes = File.ReadAllBytes(file);
                        
                        if (UseCompression)
                        {
                            using (var ms = new MemoryStream(fileBytes))
                            using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                            using (var outMs = new MemoryStream())
                            {
                                gzip.CopyTo(outMs);
                                val = Encoding.UTF8.GetString(outMs.ToArray());
                            }
                        }
                        else
                        {
                            val = Encoding.UTF8.GetString(fileBytes);
                        }

                        val = Encryption.Decrypt(val, MachineInfo.Hash(32));
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Persistence: Invalid format or corrupted file for [{key}]. Deleting file. Error: {ex.Message}");
                        if (File.Exists(file)) File.Delete(file);
                        return default(T);
                    }
                }

                if (string.IsNullOrEmpty(val)) return default(T);

                return _json.Deserialize<T>(val);
            }
            catch (Exception ex)
            {
                _logger.Error($"Persistence: Failure in deserialize key [{key}]: {ex.Message}");
                return default(T);
            }
        }

        public void Save<T>(string key, T value, string subDir = "Data")
        {
            try
            {
                if (string.IsNullOrEmpty(_basePath)) return;

                string dataPath = Path.Combine(_basePath, subDir);
                if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
                
                if (_cleanupDays > 0)
                {
                    WindowsHelper.FileCleaner(dataPath, _cleanupDays);
                }

                string serializedData = _json.Serialize(value);
                string encryptedData = Encryption.Encrypt(serializedData, MachineInfo.Hash(32));
                byte[] rawBytes = Encoding.UTF8.GetBytes(encryptedData);

                if (UseCompression)
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                        {
                            gzip.Write(rawBytes, 0, rawBytes.Length);
                        }
                        File.WriteAllBytes(Path.Combine(dataPath, key + Ext), ms.ToArray());
                    }
                }
                else
                {
                    File.WriteAllBytes(Path.Combine(dataPath, key + Ext), rawBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Persistence: Failure in saving key [{key}]: {ex.Message}");
            }
        }

        public void Delete(string key, string subDir = "Data")
        {
            try
            {
                if (string.IsNullOrEmpty(_basePath)) return;

                string file = Path.Combine(_basePath, subDir, key + Ext);

                if (File.Exists(file))
                {
                    File.Delete(file);
                    _logger.Debug($"Persistence: Variable [{key}] deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Persistence: Failure in deleting key [{key}]: {ex.Message}");
            }
        }
    }
}
