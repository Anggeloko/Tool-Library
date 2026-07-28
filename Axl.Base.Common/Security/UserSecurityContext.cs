﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using Axl.Base.Models;

namespace Axl.Base.Security
{
    /// <summary>
    /// Gestiona un contexto de seguridad de usuario, permitiendo la suplantación (impersonation).
    /// </summary>
    public class UserSecurityContext : IDisposable
    {
        #region Constantes y P/Invoke

        private const int LOGON32_PROVIDER_DEFAULT = 0;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, IntPtr lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        #endregion

        private readonly IntPtr _userToken = IntPtr.Zero;
        private readonly bool _isImpersonating = false;
        private bool _disposed = false;

        public Scope OperationScope { get; private set; }

        public enum Scope
        {
            CurrentUserSession = 0,
            CurrentUserPermanent = 1,
            LocalMachine = 2
        }

        /// <summary>
        /// Crea un contexto para el usuario actual o la máquina local.
        /// </summary>
        public UserSecurityContext(Scope scope)
        {
            OperationScope = scope;
            _isImpersonating = false;

            if (scope == Scope.LocalMachine && !IsRunningAsAdmin())
            {
                throw new SecurityException("Se requieren privilegios de Administrador para operar en el scope LocalMachine.");
            }
        }

        /// <summary>
        /// Crea un contexto suplantando a un usuario específico.
        /// </summary>
        public UserSecurityContext(string username, SecureString password, LogonType logonType, string domain = ".", Scope persistenceScope = Scope.CurrentUserSession)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            if (password == null) throw new ArgumentNullException(nameof(password));

            OperationScope = persistenceScope;

            IntPtr passwordPtr = IntPtr.Zero;
            try
            {
                passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
                if (!LogonUser(username, domain, passwordPtr, (int)logonType, LOGON32_PROVIDER_DEFAULT, out _userToken))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Error in login '{username}' (LogonUser). Win32 Error Code: {errorCode}.");
                }
                _isImpersonating = true;
            }
            finally
            {
                if (passwordPtr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
            }
        }

        public DataProtectionScope DpapiScope
        {
            get { return (OperationScope == Scope.LocalMachine) ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser; }
        }

        /// <summary>
        /// Ejecuta una acción dentro del contexto de seguridad (suplantado si corresponde).
        /// </summary>
        public T Run<T>(Func<T> functionToRun)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UserSecurityContext));

            if (_isImpersonating)
            {
                using (WindowsIdentity identity = new WindowsIdentity(_userToken))
                {
                    using (WindowsImpersonationContext context = identity.Impersonate())
                    {
                        try
                        {
                            return functionToRun();
                        }
                        finally
                        {
                            context.Undo();
                        }
                    }
                }
            }
            else
            {
                return functionToRun();
            }
        }

        /// <summary>
        /// Ejecuta una acción (void) dentro del contexto de seguridad.
        /// </summary>
        public void Run(Action actionToRun)
        {
            Run(() => {
                actionToRun();
                return true;
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_userToken != IntPtr.Zero)
                {
                    CloseHandle(_userToken);
                }
                _disposed = true;
            }
        }

        ~UserSecurityContext()
        {
            Dispose(false);
        }

        private static bool IsRunningAsAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}

