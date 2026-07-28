# Tools.Background

Librería para la gestión estandarizada de hilos y tareas en segundo plano en servicios de Windows y aplicaciones de larga ejecución.

## Características
- **Multi-Framework**: Compatible con .NET 4.0 y .NET 4.5.2+.
- **Protección de Re-entrada**: Evita que una tarea se ejecute sobre sí misma si el trabajo tarda más que el intervalo del timer.
- **Async & Sync**: Soporte nativo para delegados `Action` y `Func<Task>`.
- **Apagado Seguro (Graceful Shutdown)**: Orquestador que espera a que las tareas terminen su ciclo actual antes de liberar los hilos.

## Componentes Principales

### BackgroundServiceManager

Actúa como el contenedor y orquestador del servicio.

- **`StartAll()`**: Inicia todos los timers registrados.
- **`StopAll(int timeoutMs)`**: Implementa el patrón de parada segura:
    1. Detiene el disparo de nuevos eventos.
    2. Entra en un bucle de polling esperando a que los hilos activos liberen su flag `IsRunning`.
    3. Si se supera el timeout (def: 30s), fuerza el cierre y loguea una advertencia.

### BackgroundTask

Representa una unidad de trabajo periódica.

- **`WorkSync / WorkAsync`**: El delegado que contiene la lógica de negocio.
- **`IntervalProvider`**: Función (usualmente de `SchedulerUtils`) que define cuándo se ejecutará la próxima vez.

## Ejemplo de uso en un Servicio de Windows

```csharp
public partial class MyService : ServiceBase
{
    private BackgroundServiceManager _manager;

    protected override void OnStart(string[] args)
    {
        _manager = new BackgroundServiceManager(_log);

        // Tarea A: Cada 10 minutos
        _manager.AddTask(new BackgroundTask("Acquisitor") {
            WorkAsync = async () => await _engine.DoHeavyWork(),
            IntervalProvider = () => SchedulerUtils.GetNextMinuteInterval(10)
        });

        // Tarea B: Kill Switch (Reinicio a las 3 AM)
        var killTask = new BackgroundTask("Restart", SchedulerUtils.GetDailyRunAt(3)) {
            WorkSync = () => _manager.InvokeRequestStop()
        };
        _manager.AddTask(killTask);

        // Suscribirse a la petición de parada
        _manager.RequestStop += () => this.Stop();

        _manager.StartAll();
    }

    protected override void OnStop()
    {
        _manager.StopAll(30000); // 30s de gracia
    }
}
```

## Beneficios
1. **Logs Centralizados**: Se integra con `ILog` para reportar el inicio, fin y errores de cada tarea automáticamente.
2. **Exception Handling**: Utiliza `ExceptionUtils` para desempaquetar errores anidados, facilitando el debugging.
3. **Mantenibilidad**: Elimina los cientos de líneas de código repetitivo de wrappers de timers en cada `Service1.cs`.

---
*Versión: 1.1.3*
