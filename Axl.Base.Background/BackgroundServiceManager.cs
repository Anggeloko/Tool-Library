﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Axl.Base.Interfaces;

namespace Axl.Base.Background
{
    public class BackgroundServiceManager
    {
        private readonly List<BackgroundTask> _tasks = new List<BackgroundTask>();
        private readonly ILog _log;
        private volatile bool _isStopping;

        public event Action RequestStop;

        public BackgroundServiceManager(ILog log)
        {
            _log = log;
        }

        public void AddTask(BackgroundTask task)
        {
            if (task.Log == null) task.Log = _log;
            _tasks.Add(task);
        }

        public void StartAll()
        {
            _isStopping = false;
            _log?.Info("[ServiceManager] Starting all tasks...");
            foreach (var task in _tasks)
            {
                task.Start();
            }
        }

        public void StopAll(int timeoutms = 30000)
        {
            if (_isStopping) return;
            _isStopping = true;

            _log?.Info("[ServiceManager] Stopping all tasks...");
            foreach (var task in _tasks)
            {
                task.Stop();
            }

            _log?.Info("[ServiceManager] Waiting for active tasks to finish...");
            int pollIntervalms = 200;
            int currentWait = 0;

            while (_tasks.Any(t => t.IsRunning))
            {
                if (currentWait >= timeoutms)
                {
                    var active = string.Join(", ", _tasks.Where(t => t.IsRunning).Select(t => t.Name));
                    _log?.Warn($"[ServiceManager] Shutdown timeout. Forcing close. Active tasks: {active}");
                    break;
                }

                Thread.Sleep(pollIntervalms);
                currentWait += pollIntervalms;
            }

            foreach (var task in _tasks)
            {
                task.Dispose();
            }
            _log?.Info("[ServiceManager] Service stopped.");
        }

        /// <summary>
        /// Gatillo para que una tarea pueda solicitar la parada del servicio (Kill Switch).
        /// </summary>
        public void InvokeRequestStop()
        {
            _log?.Info("[ServiceManager] Stop requested by task.");
            RequestStop?.Invoke();
        }
    }
}

