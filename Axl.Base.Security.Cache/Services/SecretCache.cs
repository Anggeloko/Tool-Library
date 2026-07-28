using System;
using System.Collections.Generic;
using Axl.Base.Models;
using Axl.Base.Security.Cache.Models;

namespace Axl.Base.Security.Cache.Services
{
    /// <summary>
    /// Thread-safe static cache for managing temporary in-memory lifecycles of SecretValue objects.
    /// </summary>
    public static class SecretCache
    {
        private static readonly Dictionary<string, CachedSecret> _cache = new Dictionary<string, CachedSecret>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        /// <summary>
        /// Retrieves a secret from the static cache, or fetches it using the retrieve delegate if missing or expired.
        /// </summary>
        public static SecretValue GetOrAdd(string key, TimeSpan ttl, Func<SecretValue> retrieveFunc)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Cache key cannot be empty.", nameof(key));
            if (retrieveFunc == null) throw new ArgumentNullException(nameof(retrieveFunc));

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var cached))
                {
                    if (!cached.IsExpired)
                    {
                        return cached.DecryptValue();
                    }

                    // Expired: Dispose the old one and remove from cache
                    cached.Dispose();
                    _cache.Remove(key);
                }

                // Call the delegate to get the fresh value
                SecretValue freshValue = retrieveFunc();
                if (freshValue == null) return null;

                // Cache a separate copy internally, and return the freshValue to the caller
                var cachedSecret = new CachedSecret(new SecretValue(freshValue.GetBytes()), ttl);
                _cache[key] = cachedSecret;

                return freshValue;
            }
        }

        /// <summary>
        /// Explicitly removes and zero-fills a secret from the cache.
        /// </summary>
        public static void Invalidate(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var cached))
                {
                    cached.Dispose();
                    _cache.Remove(key);
                }
            }
        }

        /// <summary>
        /// Clears all cached secrets and zero-fills their memory.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                foreach (var cached in _cache.Values)
                {
                    cached.Dispose();
                }
                _cache.Clear();
            }
        }
    }
}
