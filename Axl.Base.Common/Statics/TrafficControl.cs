using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Axl.Base.Statics
{
    public static class TrafficControl
    {
        private static readonly ConcurrentDictionary<string, ReaderWriterLockSlim> _locks = new ConcurrentDictionary<string, ReaderWriterLockSlim>();
        // Tiempo maximo que esperaremos por una llave antes de rendirnos
        public static int MaxWaitTimeMs { get; set; } = 15000;
        private static ReaderWriterLockSlim GetLock(string deviceId)
        {
            return _locks.GetOrAdd(deviceId, new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));
        }
        public static bool StartExclusiveHeavy(string deviceId)
        {
            return TryStartExclusiveHeavy(deviceId, MaxWaitTimeMs);
        }
        public static bool TryStartExclusiveHeavy(string deviceId, int timeoutMs)
        {
            if (!GetLock(deviceId).TryEnterWriteLock(timeoutMs))
            {
                return false;
            }
            return true;
        }
        public static void StopExclusiveHeavy(string deviceId)
        {
            var l = GetLock(deviceId);
            if (l.IsWriteLockHeld) l.ExitWriteLock();
        }
        // El mismo concepto para el modo Light...
        public static bool StartParallelLight(string deviceId)
        {
            if (!GetLock(deviceId).TryEnterReadLock(MaxWaitTimeMs)) return false;
            return true;
        }
        public static void StopParallelLight(string deviceId)
        {
            var l = GetLock(deviceId);
            if (l.IsReadLockHeld) l.ExitReadLock();
        }
        public static void RemoveDevice(string deviceId)
        {
            // 1. Intentamos sacar el objeto del diccionario
            if (_locks.TryRemove(deviceId, out ReaderWriterLockSlim lockObj))
            {
                try
                {
                    // 2. IMPORTANTE: Liberar el recurso del sistema operativo
                    // Solo si nadie lo esta usando en este milisegundo
                    lockObj.Dispose();
                }
                catch (Exception ex)
                {
                    // Si alguien lo tenia bloqueado justo ahora, aqui lo capturas
                    System.Diagnostics.Debug.WriteLine($"Error liberando lock de {deviceId}: {ex.Message}");
                }
            }
        }
    }
}

