# Axl.Base.RegistryStore

Library for secure storage of DPAPI-encrypted machine-level secrets and passwords in the Windows Registry.

## Prerequisites
- **Framework:** .NET Framework 4.0 or higher.
- **Dependencies:** `System.Security` for DPAPI encryption (`ProtectedData`).

## Technical Reference (API)

### Interface `ISecretStore`
Standard contract exposed by `Axl.Base.Interfaces` for dependency injection and storage decoupling.

```csharp
public interface ISecretStore
{
    void StoreSecret(string keyName, string secretText);
    void StoreSecret(string keyName, SecretValue secret);
    string RetrieveSecret(string keyName);
    SecretValue RetrieveSecretSecure(string keyName);
    void DeleteSecret(string keyName);
}
```

---

### Class `SecretValue`
Wrapper class for sensitive data that ensures internal byte arrays are overwritten with zeroes upon destruction or disposal.

- **Methods:**
  - `SecretValue(string secretText)`: Creates instance from a string.
  - `SecretValue(byte[] utf8Bytes)`: Creates instance from a byte array.
  - `GetString()`: Decodes and returns the secret as a string.
  - `GetBytes()`: Returns a copy of the underlying byte array.
  - `Dispose()`: Clears and zero-fills internal byte arrays.

---

### Class `RegistryKeyStore`
Implements `ISecretStore` and handles physical writing to Registry keys under `HKLM` (64-bit Registry), automatically managing Windows read/write permissions (ACLs).

#### Usage Example (Installer / Elevated Process)
Encrypts and stores an API key, granting read-only permissions to local service account `.\MikeUser`.
```csharp
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.RegistryStore.Services;

// Initialization step (Requires running as Administrator)
ISecretStore store = new RegistryKeyStore(@"SOFTWARE\SchneiderElectric\ServiceManager", null, @".\MikeUser");

// Securely store secret using SecretValue
using (var secret = new SecretValue("SuperSecurePassword123!"))
{
    store.StoreSecret("DatabaseConnectionString", secret);
}
```

#### Usage Example (Background Service / Reader Process)
The service running under the `.\MikeUser` account can read the key without requiring elevated Administrator privileges or knowing DPAPI entropy secrets:
```csharp
ISecretStore store = new RegistryKeyStore(@"SOFTWARE\SchneiderElectric\ServiceManager");

using (SecretValue secret = store.RetrieveSecretSecure("DatabaseConnectionString"))
{
    string connString = secret.GetString();
    // Connection usage...
} // Disposal automatically zeroes out the memory buffer
```

#### `RegistryKeyStore(string registryPath, byte[] entropy = null, string serviceAccountName = null)`
- **Parameters:**
  - `registryPath`: The Registry key path under HKLM (e.g. `SOFTWARE\MyCompany\App`).
  - `entropy`: Optional byte array adding an extra layer of secrecy to local DPAPI encryption. Must match when writing and reading.
  - `serviceAccountName`: Account name (e.g. `.\ServiceUser` or `MYDOMAIN\User`) that will explicitly receive read permissions on the Registry key.

#### `void DeleteStoreTree()`
Recursively deletes the entire subkey tree of this store under HKLM.
> [!IMPORTANT]
> This method includes security guardrails and will throw a `SecurityException` if the registry path is too short or does not start with `SOFTWARE` (requires at least 3 levels of depth, e.g., `SOFTWARE\Company\App`), preventing accidental deletion of critical system branches.
