using System;
using System.Security.Cryptography;
using Axl.Base.Models;

namespace Axl.Base.Security.Cache.Models
{
    /// <summary>
    /// Represents a cached secret encrypted in memory using DPAPI.
    /// </summary>
    internal class CachedSecret : IDisposable
    {
        private byte[] _encryptedValue;
        private static readonly byte[] CacheEntropy = { 99, 12, 44, 76, 33, 88, 11, 25 };

        public DateTime ExpirationTime { get; }

        public CachedSecret(SecretValue value, TimeSpan ttl)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            byte[] rawBytes = value.GetBytes();
            try
            {
                // Encrypt bytes in memory immediately using DPAPI LocalMachine scope
                _encryptedValue = ProtectedData.Protect(rawBytes, CacheEntropy, DataProtectionScope.LocalMachine);
            }
            finally
            {
                // Clear the raw bytes immediately
                Array.Clear(rawBytes, 0, rawBytes.Length);
            }

            ExpirationTime = DateTime.UtcNow.Add(ttl);
        }

        /// <summary>
        /// Gets whether this cached secret has expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpirationTime;

        /// <summary>
        /// Decrypts the secret value from memory at the moment of request.
        /// </summary>
        public SecretValue DecryptValue()
        {
            if (_encryptedValue == null)
                throw new ObjectDisposedException(nameof(CachedSecret));

            byte[] decryptedBytes = ProtectedData.Unprotect(_encryptedValue, CacheEntropy, DataProtectionScope.LocalMachine);
            try
            {
                return new SecretValue(decryptedBytes);
            }
            finally
            {
                // Clear the temporary decrypted buffer immediately
                Array.Clear(decryptedBytes, 0, decryptedBytes.Length);
            }
        }

        /// <summary>
        /// Clears the encrypted secret from memory.
        /// </summary>
        public void Dispose()
        {
            if (_encryptedValue != null)
            {
                Array.Clear(_encryptedValue, 0, _encryptedValue.Length);
                _encryptedValue = null;
            }
        }
    }
}
