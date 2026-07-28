using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Axl.Base.Statics
{
    /// <summary>
    /// Robust AES-256 encryption helper that is easily replicable in other services.
    /// It uses a hardware-based hash derived from MachineInfo.
    /// </summary>
    public static class Encryption
    {
        private static readonly byte[] Salt = Encoding.ASCII.GetBytes("SchneiderElectricServiceManagerSalt");

        public static string Encrypt(string plainText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            byte[] outBuffer;
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(sharedSecret, Salt, 1000);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(bytes, 0, bytes.Length);
                        cs.FlushFinalBlock();
                    }
                    outBuffer = ms.ToArray();
                }
            }
            return Convert.ToBase64String(outBuffer);
        }

        public static string Decrypt(string cipherText, string sharedSecret)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";

            try
            {
                byte[] bytes = Convert.FromBase64String(cipherText);
                using (var aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(sharedSecret, Salt, 1000);
                    aes.Key = key.GetBytes(aes.KeySize / 8);
                    aes.IV = key.GetBytes(aes.BlockSize / 8);

                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(bytes, 0, bytes.Length);
                            cs.FlushFinalBlock();
                        }
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch
            {
                return "[Decryption Error]";
            }
        }

        /// <summary>
        /// Intenta desencriptar un texto usando un pool de posibles llaves de la máquina.
        /// Útil cuando la identidad de la máquina cambia ligeramente (ej. cambio de nombre o red).
        /// </summary>
        public static string DecryptWithPool(string cipherText, List<string> keyPool)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";
            if (keyPool == null || keyPool.Count == 0) return cipherText;

            foreach (var key in keyPool)
            {
                string result = Decrypt(cipherText, key);
                if (result != "[Decryption Error]")
                {
                    return result; // Encontramos la llave correcta
                }
            }

            return "[Decryption Error]";
        }
    }

}

