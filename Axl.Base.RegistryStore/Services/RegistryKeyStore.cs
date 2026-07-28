using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Security;
using System.Text;
using Axl.Base.Interfaces;
using Axl.Base.Models;

namespace Axl.Base.RegistryStore.Services
{
    /// <summary>
    /// Implementation of a secret store inside the Windows Registry using DPAPI.
    /// </summary>
    public class RegistryKeyStore : ISecretStore
    {
        private readonly string _registryBasePath;
        private readonly byte[] _additionalEntropy;
        private readonly string _serviceAccountName;

        // Default fallback entropy if none is supplied.
        private static readonly byte[] DefaultEntropy = { 12, 55, 99, 23, 44, 11, 88, 100 };

        /// <summary>
        /// Creates an instance of the Windows Registry KeyStore.
        /// </summary>
        /// <param name="registryPath">Base registry path under HKLM (e.g. SOFTWARE\MyCompany\App).</param>
        /// <param name="entropy">Optional bytes for additional DPAPI entropy. If null, a default fallback is used.</param>
        /// <param name="serviceAccountName">Optional service account or user name that will receive read permissions.</param>
        public RegistryKeyStore(string registryPath, byte[] entropy = null, string serviceAccountName = null)
        {
            if (string.IsNullOrWhiteSpace(registryPath))
                throw new ArgumentException("Registry path can't be empty", nameof(registryPath));

            _registryBasePath = registryPath;
            _additionalEntropy = entropy ?? DefaultEntropy;
            _serviceAccountName = serviceAccountName;
        }

        /// <summary>
        /// Stores a plain-text secret under the specified key.
        /// </summary>
        public void StoreSecret(string keyName, string secretText)
        {
            if (string.IsNullOrEmpty(keyName)) throw new ArgumentNullException(nameof(keyName));
            if (string.IsNullOrEmpty(secretText)) throw new ArgumentNullException(nameof(secretText));

            byte[] secretBytes = null;
            byte[] encryptedData = null;

            try
            {
                secretBytes = Encoding.UTF8.GetBytes(secretText);
                encryptedData = ProtectedData.Protect(secretBytes, _additionalEntropy, DataProtectionScope.LocalMachine);
                SaveToRegistry(keyName, encryptedData);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failed to encrypt secret.", ex);
            }
            finally
            {
                if (secretBytes != null) Array.Clear(secretBytes, 0, secretBytes.Length);
            }
        }

        /// <summary>
        /// Stores a SecretValue object securely under the specified key.
        /// </summary>
        public void StoreSecret(string keyName, SecretValue secret)
        {
            if (string.IsNullOrEmpty(keyName)) throw new ArgumentNullException(nameof(keyName));
            if (secret == null) throw new ArgumentNullException(nameof(secret));

            byte[] secretBytes = null;
            byte[] encryptedData = null;

            try
            {
                secretBytes = secret.GetBytes();
                encryptedData = ProtectedData.Protect(secretBytes, _additionalEntropy, DataProtectionScope.LocalMachine);
                SaveToRegistry(keyName, encryptedData);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Failed to encrypt SecretValue.", ex);
            }
            finally
            {
                if (secretBytes != null) Array.Clear(secretBytes, 0, secretBytes.Length);
            }
        }

        /// <summary>
        /// Retrieves a plain-text secret. Returns null if it doesn't exist.
        /// </summary>
        public string RetrieveSecret(string keyName)
        {
            byte[] encryptedData = ReadFromRegistry(keyName);
            if (encryptedData == null) return null;

            byte[] decryptedBytes = null;
            try
            {
                decryptedBytes = ProtectedData.Unprotect(encryptedData, _additionalEntropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch { return null; }
            finally
            {
                if (decryptedBytes != null) Array.Clear(decryptedBytes, 0, decryptedBytes.Length);
            }
        }

        /// <summary>
        /// Retrieves a secret wrapped in a SecretValue object.
        /// </summary>
        public SecretValue RetrieveSecretSecure(string keyName)
        {
            byte[] encryptedData = ReadFromRegistry(keyName);
            if (encryptedData == null) return null;

            byte[] decryptedBytes = null;
            try
            {
                decryptedBytes = ProtectedData.Unprotect(encryptedData, _additionalEntropy, DataProtectionScope.LocalMachine);
                return new SecretValue(decryptedBytes);
            }
            catch { return null; }
            finally
            {
                if (decryptedBytes != null) Array.Clear(decryptedBytes, 0, decryptedBytes.Length);
            }
        }

        /// <summary>
        /// Deletes a single secret value from the registry.
        /// </summary>
        public void DeleteSecret(string keyName)
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key = baseKey.OpenSubKey(_registryBasePath, true))
            {
                if (key != null) key.DeleteValue(keyName, false);
            }
        }

        /// <summary>
        /// Recursively deletes the entire subkey tree of this store from HKLM.
        /// </summary>
        public void DeleteStoreTree()
        {
            if (string.IsNullOrWhiteSpace(_registryBasePath))
                throw new InvalidOperationException("Registry base path cannot be empty.");

            string[] parts = _registryBasePath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            // Safety Guardrail: Force at least 3 parts (e.g. SOFTWARE\Company\App) to prevent root folder deletions
            if (parts.Length < 3 || !string.Equals(parts[0], "SOFTWARE", StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException("Operation denied: The registry path is too short or insecure for recursive deletion.");
            }

            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                baseKey.DeleteSubKeyTree(_registryBasePath, false);
            }
        }

        // --- Private Methods ---

        private void SaveToRegistry(string keyName, byte[] data)
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (var key = baseKey.CreateSubKey(_registryBasePath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    key.SetValue(keyName, data, RegistryValueKind.Binary);
                    SetRegistryAcl(key);
                }
            }
        }

        private byte[] ReadFromRegistry(string keyName)
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                // RegistryRights.ReadKey is used to read without requiring excessive permissions.
                using (var key = baseKey.OpenSubKey(_registryBasePath, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
                {
                    return key?.GetValue(keyName) as byte[];
                }
            }
        }

        private void SetRegistryAcl(RegistryKey key)
        {
            try
            {
                var security = new RegistrySecurity();

                // false = DO NOT protect inherited access rules.
                // true = Retain inherited rules from parent.
                security.SetAccessRuleProtection(false, true);

                // SYSTEM
                security.AddAccessRule(new RegistryAccessRule(
                    new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                    RegistryRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                // Administrators
                security.AddAccessRule(new RegistryAccessRule(
                    new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                    RegistryRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                // If a specific service account was provided
                if (!string.IsNullOrWhiteSpace(_serviceAccountName))
                {
                    try
                    {
                        string targetAccount = _serviceAccountName;
                        if (targetAccount.StartsWith(@".\"))
                        {
                            string nameOnly = targetAccount.Substring(2);
                            targetAccount = $"{Environment.MachineName}\\{nameOnly}";
                        }

                        var account = new NTAccount(targetAccount);
                        var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));

                        // Add read-only permission (ReadKey)
                        security.AddAccessRule(new RegistryAccessRule(
                            sid,
                            RegistryRights.ReadKey | RegistryRights.QueryValues | RegistryRights.EnumerateSubKeys | RegistryRights.ReadPermissions,
                            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                            PropagationFlags.None,
                            AccessControlType.Allow));
                    }
                    catch (IdentityNotMappedException)
                    {
                        throw new InvalidOperationException($"The service account '{_serviceAccountName}' was not found in the system.");
                    }
                }

                key.SetAccessControl(security);
            }
            catch (UnauthorizedAccessException)
            {
                // Admin privileges are required to configure access rules
                throw new InvalidOperationException("You must run the application with elevated Administrator privileges to set registry security permissions.");
            }
        }
    }
}
