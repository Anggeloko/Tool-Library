﻿using System;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Statics;
using System.Timers;

namespace Axl.Base.Background
{
    public class BackgroundTask : IDisposable
    {
        private readonly Timer _timer;
        private volatile bool _isRunning;
        private bool _isDisposed;

        public string Name { get; }
        public ILog Log { get; set; }
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Síncrono: Acción a ejecutar.
        /// </summary>
        public Action WorkSync { get; set; }

        /// <summary>
        /// Asíncrono: Función que retorna una Tarea.
        /// </summary>
        public Func<System.Threading.Tasks.Task> WorkAsync { get; set; }

        /// <summary>
        /// Proveedor del próximo intervalo en milisegundos.
        /// </summary>
        public Func<double> IntervalProvider { get; set; }

        public BackgroundTask(string name, double initialIntervalms = 1000)
        {
            Name = name;
            _timer = new Timer(initialIntervalms);
            _timer.AutoReset = false; // Manejamos el reinicio manualmente
            _timer.Elapsed += OnElapsed;
        }

        public void Start()
        {
            if (_isDisposed) return;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private async void OnElapsed(object sender, ElapsedEventArgs e)
        {
            // Intentamos obtener el bloqueo exclusivo de forma inmediata (0ms de espera)
            if (!TrafficControl.TryStartExclusiveHeavy(Name, 0))
            {
                return; // Ya está corriendo o el recurso está ocupado
            }

            _isRunning = true;
            try
            {
                Log?.Info($"[{Name}] START");

                if (WorkAsync != null)
                {
                    await WorkAsync().ConfigureAwait(false);
                }
                else if (WorkSync != null)
                {
                    WorkSync();
                }
            }
            catch (Exception ex)
            {
                Log?.Error($"[{Name}] Error: {ex.Message}");
                Log?.Error(ExceptionUtils.Format(ex, Name));
            }
            finally
            {
                Log?.Info($"[{Name}] END");

                _isRunning = false;
                TrafficControl.StopExclusiveHeavy(Name);

                if (IntervalProvider != null)
                {
                    var next = IntervalProvider();
                    if (next > 0)
                    {
                        _timer.Interval = next;
                        _timer.Start();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _timer.Stop();
            _timer.Dispose();
        }
    }
}

