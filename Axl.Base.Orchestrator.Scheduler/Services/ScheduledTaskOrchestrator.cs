using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Axl.Base.Background;
using Axl.Base.Interfaces;
using Axl.Base.Orchestrator.Scheduler.Ports;

namespace Axl.Base.Orchestrator.Scheduler.Services
{
    public class ScheduledTaskOrchestrator : IDisposable
    {
        private readonly BackgroundServiceManager _serviceManager;
        private readonly ITimerConfigProvider _configProvider;
        private readonly List<IScheduledTaskHandler> _handlers;
        private readonly ILog _log;

        public ScheduledTaskOrchestrator(
            BackgroundServiceManager serviceManager,
            ITimerConfigProvider configProvider,
            IEnumerable<IScheduledTaskHandler> handlers,
            ILog log = null)
        {
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _handlers = handlers != null ? handlers.ToList() : new List<IScheduledTaskHandler>();
            _log = log;
        }

        public void RegisterHandler(IScheduledTaskHandler handler)
        {
            if (handler == null) return;
            lock (_handlers)
            {
                if (!_handlers.Contains(handler))
                {
                    _handlers.Add(handler);
                    _log?.Info($"[ScheduledTaskOrchestrator] Dynamic handler '{handler.TaskName}' registered.");
                }
            }
        }

        public void RegisterTask(IScheduledTaskHandler handler, string categoryOrArea)
        {
            RegisterHandler(handler);
            RegisterTask(handler.TaskName, categoryOrArea);
        }

        public void RegisterTask(string taskName, string categoryOrArea)
        {
            IScheduledTaskHandler handler;
            lock (_handlers)
            {
                handler = _handlers.FirstOrDefault(h => h.TaskName.Equals(taskName, StringComparison.OrdinalIgnoreCase) && h.CanHandle(categoryOrArea));
            }
            if (handler == null)
            {
                _log?.Warn($"[ScheduledTaskOrchestrator] No handler found for TaskName: '{taskName}', Category: '{categoryOrArea}'");
                return;
            }

            double initialInterval = _configProvider.GetIntervalMs(taskName, categoryOrArea);
            if (initialInterval <= 0) initialInterval = 5000;

            var bgTask = new BackgroundTask($"{taskName}_{categoryOrArea}", initialInterval)
            {
                Log = _log,
                IntervalProvider = () =>
                {
                    if (!_configProvider.IsEnabled(taskName, categoryOrArea))
                    {
                        return -1; // Deshabilita la reprogramación
                    }
                    double next = _configProvider.GetIntervalMs(taskName, categoryOrArea);
                    return next > 0 ? next : initialInterval;
                },
                WorkAsync = async () =>
                {
                    if (!_configProvider.IsEnabled(taskName, categoryOrArea))
                    {
                        _log?.Info($"[ScheduledTaskOrchestrator] Task '{taskName}' ({categoryOrArea}) is currently disabled. Skipping execution.");
                        return;
                    }

                    _log?.Info($"[ScheduledTaskOrchestrator] Executing '{taskName}' for category '{categoryOrArea}'...");
                    using (var cts = new CancellationTokenSource())
                    {
                        await handler.ExecuteAsync(categoryOrArea, cts.Token).ConfigureAwait(false);
                    }
                }
            };

            _serviceManager.AddTask(bgTask);
            _log?.Info($"[ScheduledTaskOrchestrator] Registered scheduled task '{taskName}' [{categoryOrArea}] with initial interval {initialInterval}ms.");
        }

        public void StartAll()
        {
            _serviceManager.StartAll();
        }

        public void StopAll(int timeoutMs = 30000)
        {
            _serviceManager.StopAll(timeoutMs);
        }

        public void Dispose()
        {
            StopAll();
        }
    }
}
