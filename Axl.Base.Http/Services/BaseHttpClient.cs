﻿using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Axl.Base.Tests")]

namespace Axl.Base.Http.Services
{
    public abstract class BaseHttpClient : IDisposable
    {
        private static readonly HttpClient _httpClient;
        private static bool _devModeEnabled = false;

        static BaseHttpClient()
        {
            // Subimos el límite de conexiones simultáneas al mismo host (Default en .NET Framework es 2)
            System.Net.ServicePointManager.DefaultConnectionLimit = 512;

            // Protocolos estándar
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler);
        }

        protected HttpClient Client => _httpClient;
        private static HttpClient _injectedClient;

        internal static void SetClientForTesting(HttpClient client)
        {
            _injectedClient = client;
        }

        protected HttpClient GetClient() => _injectedClient ?? _httpClient;

        /// <summary>
        /// Activa el modo desarrollador de forma global. 
        /// ADVERTENCIA: Esto omitirá la validación de certificados SSL en toda la aplicación.
        /// </summary>
        protected void EnsureDeveloperMode(bool enable)
        {
            if (enable && !_devModeEnabled)
            {
                lock (_httpClient)
                {
                    if (!_devModeEnabled)
                    {
                        // PELIGROSO: SOLO PARA DESARROLLO. Omite la validación de certificados.
                        System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                        _devModeEnabled = true;
                    }
                }
            }
        }

        public virtual void Dispose()
        {
        }
    }
}

