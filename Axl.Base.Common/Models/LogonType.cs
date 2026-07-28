﻿namespace Axl.Base.Models
{
    /// <summary>
    /// Tipos de inicio de sesión de Windows.
    /// </summary>
    public enum LogonType
    {
        /// <summary>
        /// Inicio de sesión interactivo (para usuarios locales).
        /// </summary>
        Interactive = 2,

        /// <summary>
        /// Inicio de sesión de red (más rápido para acceso a archivos).
        /// </summary>
        Network = 3,

        /// <summary>
        /// Permite al proceso actual continuar su ejecución, pero suplantando 
        /// las credenciales para llamadas de red salientes.
        /// </summary>
        NewCredentials = 9
    }
}

