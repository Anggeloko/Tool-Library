using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Axl.Base.Models;

namespace Axl.Base.Statics
{
    /// <summary>
    /// Proporciona métodos auxiliares relacionados con la autenticación y operaciones de Windows.
    /// </summary>
    public static class WindowsHelper
    {
        #region P/Invoke para Conexiones de Red (WNet)

        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WNetAddConnection2(
            ref NETRESOURCE lpNetResource,
            string lpPassword,
            string lpUsername,
            int dwFlags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        public static extern int WNetCancelConnection2(
            string lpName,
            int dwFlags,
            bool fForce);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NETRESOURCE
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string lpLocalName;
            public string lpRemoteName;
            public string lpComment;
            public string lpProvider;
        }

        public const int RESOURCETYPE_DISK = 0x00000001;
        public const int CONNECT_TEMPORARY = 0x00000004;
        public const int CONNECT_INTERACTIVE = 0x00000008;

        #endregion

        #region P/Invoke para LogonUser

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, IntPtr lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int LOGON32_PROVIDER_DEFAULT = 0;

        #endregion

        /// <summary>
        /// Valida credenciales intentando una conexión de red (WNet).
        /// Útil para entornos donde LogonUser no es suficiente (ej: msA).
        /// </summary>
        public static bool ValidateCredentialsWithNetwork(string usernameWithDomain, SecureString password, string remotePath)
        {
            if (string.IsNullOrEmpty(usernameWithDomain) || password == null || password.Length == 0)
                return false;

            IntPtr passPtr = IntPtr.Zero;
            string passPlainText = null;

            try
            {
                passPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
                passPlainText = Marshal.PtrToStringUni(passPtr);

                var netResource = new NETRESOURCE()
                {
                    dwType = RESOURCETYPE_DISK,
                    lpRemoteName = remotePath
                };

                int result = WNetAddConnection2(
                    ref netResource,
                    passPlainText,
                    usernameWithDomain,
                    CONNECT_TEMPORARY
                );

                bool success = (result == 0);

                if (success)
                {
                    WNetCancelConnection2(remotePath, 0, true);
                }

                return success;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (passPtr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passPtr);
            }
        }

        /// <summary>
        /// Valida activamente un conjunto de credenciales usando LogonUser.
        /// </summary>
        public static bool ValidateCredentials(string usernameWithDomain, SecureString password, LogonType logonType)
        {
            if (string.IsNullOrEmpty(usernameWithDomain) || password == null || password.Length == 0)
                return false;

            string[] parts = usernameWithDomain.Split('\\');
            string username = parts.Length > 1 ? parts[1] : parts[0];
            string domain = parts.Length > 1 ? parts[0] : ".";

            IntPtr passwordPtr = IntPtr.Zero;
            IntPtr tokenHandle = IntPtr.Zero;

            try
            {
                passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);

                bool logonSuccess = LogonUser(
                    username,
                    domain,
                    passwordPtr,
                    (int)logonType,
                    LOGON32_PROVIDER_DEFAULT,
                    out tokenHandle
                );

                return logonSuccess;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (passwordPtr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);

                if (tokenHandle != IntPtr.Zero)
                    CloseHandle(tokenHandle);
            }
        }

        /// <summary>
        /// Verifica si un nombre de usuario existe en el dominio o máquina local.
        /// </summary>
        public static bool DoesUserExist(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return false;

            try
            {
                NTAccount account = new NTAccount(userName);
                SecurityIdentifier sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
                return true;
            }
            catch (IdentityNotMappedException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida una ruta de red (UNC) para prevenir Path Traversal.
        /// </summary>
        public static string ValidateNetworkPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path can't be null or empty.", nameof(path));

            if (!path.StartsWith(@"\\"))
                throw new ArgumentException("Path must be a network UNC path (e.g., \\\\server\\share).");

            string fullCanonicalPath;
            try
            {
                fullCanonicalPath = Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Input path has invalid characters: '{path}'.", nameof(path), ex);
            }

            var pathParts = path.TrimStart('\\').Split('\\');
            if (pathParts.Length < 2)
            {
                throw new ArgumentException("UNC path must contain at least a server and a share name.");
            }

            string uncAnchor = $@"\\{pathParts[0]}\{pathParts[1]}";
            string fullUncAnchor = Path.GetFullPath(uncAnchor);

            if (!fullCanonicalPath.StartsWith(fullUncAnchor, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Path Traversal attack detected: '{path}' is outside '{fullUncAnchor}'.");
            }

            return fullCanonicalPath;
        }

        /// <summary>
        /// Valida una ruta local contra un directorio "ancla" para prevenir Path Traversal.
        /// </summary>
        public static string ValidateAnchoredPath(string path, string anchorPath)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path can't be null or empty.", nameof(path));

            if (string.IsNullOrWhiteSpace(anchorPath))
                throw new ArgumentException("Anchor path can't be null or empty.", nameof(anchorPath));

            string fullAnchorPath = Path.GetFullPath(anchorPath);
            string fullCanonicalPath = Path.GetFullPath(path);

            if (!fullCanonicalPath.StartsWith(fullAnchorPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Path Traversal attack detected: '{path}' is outside '{fullAnchorPath}'.");
            }

            return fullCanonicalPath;
        }

        /// <summary>
        /// Convierte un string normal a SecureString.
        /// </summary>
        public static SecureString ToSecureString(string plainString)
        {
            if (plainString == null) return null;
            var secureString = new SecureString();
            foreach (char c in plainString) secureString.AppendChar(c);
            secureString.MakeReadOnly();
            return secureString;
        }

        /// <summary>
        /// Convierte un SecureString a string normal (ADVERTENCIA: Expone en RAM).
        /// </summary>
        public static string ToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null) return null;
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                if (unmanagedString != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
        /// <summary>
        /// Terminates all processes with the specified name. 
        /// Useful for cleaning up orphan processes in automation tasks.
        /// </summary>
        /// <param name="processName">Name of the process to terminate (without extension).</param>
        /// <returns>A list of error messages if any operation fails.</returns>
        public static List<string> CleanZombies(string processName)
        {
            List<string> errors = new List<string>();
            try
            {
                // Find and terminate all instances of the process
                Process[] zombies = Process.GetProcessesByName(processName);
                foreach (Process p in zombies)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(500); // Allow OS to release resources
                    }
                    catch { /* Ignore access denied or already terminated errors */ }
                }
            }
            catch (Exception ex)
            {
                errors.AddRange(ExceptionUtils.Format(ex));
            }
            return errors;
        }

        /// <summary>
        /// Deletes files in a folder that are older than the specified number of days.
        /// </summary>
        /// <param name="folderPath">The path of the folder to clean.</param>
        /// <param name="daysThreshold">The age threshold in days. Files older than this will be deleted.</param>
        /// <returns>A list of error messages if any file could not be deleted.</returns>
        public static List<string> FileCleaner(string folderPath, int daysThreshold)
        {
            List<string> errors = new List<string>();
            try
            {
                // Validate folder existence
                if (!Directory.Exists(folderPath))
                {
                    errors.Add($"Directory does not exist: {folderPath}");
                    return errors;
                }

                DirectoryInfo directory = new DirectoryInfo(folderPath);

                // Calculate the cutoff date by subtracting days from current time
                DateTime cutoffDate = DateTime.Now.AddDays(-daysThreshold);

                FileInfo[] files = directory.GetFiles();

                foreach (FileInfo file in files)
                {
                    // Compare creation time with our cutoff date
                    if (file.CreationTime < cutoffDate)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (IOException ex)
                        {
                            errors.AddRange(ExceptionUtils.Format(ex));
                            // File might be locked, skip for now
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            errors.AddRange(ExceptionUtils.Format(ex));
                            // Insufficient permissions
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.AddRange(ExceptionUtils.Format(ex));
            }
            return errors;
        }

    }
}

