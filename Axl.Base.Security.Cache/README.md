# Axl.Base.Security.Cache

Timed and secure in-memory cache library for `SecretValue` objects. Provides a short, controlled lifecycle for sensitive secrets in RAM, preventing them from residing indefinitely in plain text.

## In-Memory Security (Memory Dump Mitigation)
> [!IMPORTANT]
> Unlike classical in-memory caches that store exposed values in plain text, `SecretCache` internally encrypts secret bytes in RAM using **DPAPI** (`ProtectedData.Protect` under `DataProtectionScope.LocalMachine`). 
> 
> As a result, if a process memory dump occurs, cached secrets are not exposed. They are only decrypted into an ephemeral temporary buffer during the exact microsecond of the request, which is immediately zero-filled (`Array.Clear`) after being wrapped into the returned `SecretValue`.

## Prerequisites
- **Framework:** .NET Framework 4.0 or higher.
- **Dependencies:** `Axl.Base.Common` (for `SecretValue` usage).

---

## Technical Reference (API)

### Class `SecretCache`
Thread-safe static class (`lock`) to store and retrieve `SecretValue` objects in a controlled manner.

#### `SecretValue GetOrAdd(string key, TimeSpan ttl, Func<SecretValue> retrieveFunc)`
Retrieves a secret from static in-memory cache. If the secret does not exist or has expired, it invokes the provided physical delegate, initializes and saves a cached copy, and returns the value.
* **Parameters:**
  - `key`: Unique secret identifier in the cache (case-insensitive lookup).
  - `ttl`: Time-To-Live in cache before being marked as expired and destroyed.
  - `retrieveFunc`: Delegate function executed only if the cache requires a physical refresh.
* **Return:** An independent `SecretValue` object. The caller is responsible for calling `Dispose()` when finished.

#### `void Invalidate(string key)`
Immediately invalidates and removes a specific secret from the in-memory cache, destroying and overwriting its content with zeroes.

#### `void Clear()`
Invalidates, removes, and safely zero-fills **all** in-memory secrets stored in the cache.

---

## Practical Usage Examples

### Example 1: Integration in Configuration Classes (Case `Cfg.cs` with Broker Credentials)
Instead of storing decrypted sensitive passwords or usernames permanently in static `string` properties in memory, the cache is used to request them on-demand:

```csharp
using System;
using System.Linq;
using Axl.Base.Models;
using Axl.Base.Security.Cache.Services;

public class AppConfig
{
    private readonly string _dbPath = @"C:\Data\settings.db";
    
    // Public non-sensitive parameters
    public string BrokerIp { get; set; }
    public int BrokerPort { get; set; }

    public AppConfig()
    {
        // Load common variables in constructor
        using (var sqlite = new SQLiteService(_dbPath))
        {
            var configList = sqlite.ReadVariables();
            
            BrokerIp = configList.FirstOrDefault(x => x.Key == "BROKERIP")?.Value ?? "127.0.0.1";
            
            if (int.TryParse(configList.FirstOrDefault(x => x.Key == "BROKERPORT")?.Value, out int port))
                BrokerPort = port;

            foreach (var cfg in configList) cfg.Dispose();
        }
    }

    /// <summary>
    /// Securely retrieves Broker Username from temporary cache
    /// </summary>
    public SecretValue GetBrokerUser()
    {
        return SecretCache.GetOrAdd("BROKERUSER", TimeSpan.FromMinutes(10), () =>
        {
            using (var sqlite = new SQLiteService(_dbPath))
            {
                // Simulated reading of encrypted database value
                string decryptedUser = sqlite.ReadRawSecret("BROKERUSER");
                return new SecretValue(decryptedUser);
            }
        });
    }

    /// <summary>
    /// Securely retrieves Broker Password from temporary cache
    /// </summary>
    public SecretValue GetBrokerPswd()
    {
        return SecretCache.GetOrAdd("BROKERPSWD", TimeSpan.FromMinutes(10), () =>
        {
            using (var sqlite = new SQLiteService(_dbPath))
            {
                string decryptedPswd = sqlite.ReadRawSecret("BROKERPSWD");
                return new SecretValue(decryptedPswd);
            }
        });
    }
}
```

#### Consuming Credentials to Connect the Client:
```csharp
public void ConnectToMqtt(AppConfig config)
{
    // Retrieve values from cache (they will only exist decrypted within this block scope)
    using (SecretValue user = config.GetBrokerUser())
    using (SecretValue password = config.GetBrokerPswd())
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(config.BrokerIp, config.BrokerPort)
            .WithCredentials(user.GetString(), password.GetString()) // Decoded and used on the fly
            .Build();

        _mqttClient.ConnectAsync(options);
    } 
    // Sensitive RAM data for 'user' and 'password' is zero-overwritten here
}
```

---

### Example 2: Direct Usage with Dependency Injection
```csharp
using System;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.RegistryStore.Services;
using Axl.Base.Security.Cache.Services;

public class DatabaseWatcher
{
    private readonly ISecretStore _store = new RegistryKeyStore(@"SOFTWARE\MyCompany\App");

    public void ProcessTask()
    {
        // Requests secret from static cache with 10-minute TTL.
        // Will only physically read Registry if missing or expired after 10 minutes.
        using (SecretValue secret = SecretCache.GetOrAdd(
            "DbConnectionString", 
            TimeSpan.FromMinutes(10), 
            () => _store.RetrieveSecretSecure("DbConnectionString")))
        {
            string connectionString = secret.GetString();
            // Execute database queries...
        } // 'secret' is destroyed and its memory bytes zeroed upon exiting block
    }
}
```
