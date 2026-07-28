﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axl.Base.Statics
{
    public static class RouteRegistry
    {
        // Estructura interna para guardar IP + Fecha
        private class RouteInfo
        {
            public string IP { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        // Diccionario seguro para hilos (Nativo en .NET 4.0)
        private static readonly ConcurrentDictionary<string, RouteInfo> _cache =
            new ConcurrentDictionary<string, RouteInfo>();

        /// <summary>
        /// Actualiza o inserta la mejor IP para un equipo.
        /// </summary>
        public static void Update(string deviceId, string ip)
        {
            var info = new RouteInfo { IP = ip, UpdatedAt = DateTime.Now };
            _cache[deviceId] = info; // El indexador de ConcurrentDictionary hace AddOrUpdate autom�ticamente
        }

        /// <summary>
        /// Obtiene la IP guardada. Si no existe, devuelve null.
        /// </summary>
        public static string Get(string deviceId)
        {
            if (_cache.TryGetValue(deviceId, out RouteInfo info))
            {
                return info.IP;
            }
            return null;
        }

        /// <summary>
        /// Permite saber hace cu�nto tiempo se valid� la ruta.
        /// </summary>
        public static DateTime GetLastUpdate(string deviceId)
        {
            if (_cache.TryGetValue(deviceId, out RouteInfo info))
                return info.UpdatedAt;
            return DateTime.MinValue;
        }

        /// <summary>
        /// Limpia un equipo espec�fico (�til si se borra de la DB).
        /// </summary>
        public static void Remove(string deviceId)
        {
            RouteInfo removed;
            _cache.TryRemove(deviceId, out removed);
        }

        /// <summary>
        /// Limpia toda la memoria (�til al reiniciar servicios).
        /// </summary>
        public static void ClearAll() => _cache.Clear();
    }
}

