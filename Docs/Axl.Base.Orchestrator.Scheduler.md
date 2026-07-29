# Documentación Técnica: Axl.Base.Orchestrator.Scheduler ⏱️

## 1. Descripción General
`Axl.Base.Orchestrator.Scheduler` es una librería diseñada bajo la **Arquitectura Hexagonal (Puertos y Adaptadores)** y el **Patrón Estrategia (Strategy Pattern)** para orquestar la ejecución de tareas en segundo plano programadas por tiempo.

Permite desacoplar las tareas concretas de recolección o mantenimiento (Wonderware, WMI, SNMP, ICMP) de la lógica de temporización, leyendo intervalos dinámicos y estados de activación desde un proveedor inyectado.

---

## 2. Puertos Principales (Interfaces)

### `IScheduledTaskHandler`
Puerto de entrada para la ejecución de tareas programadas.
- **`TaskName`**: Identificador de la tarea.
- **`CanHandle(string categoryOrArea)`**: Evalúa si el handler atiende una categoría específica.
- **`ExecuteAsync(string categoryOrArea, CancellationToken cancellationToken)`**: Ejecuta la lógica asíncrona.

### `ITimerConfigProvider`
Puerto secundario para la obtención de configuración de temporización.
- **`GetIntervalMs(string taskName, string categoryOrArea)`**: Devuelve el intervalo actual en milisegundos.
- **`IsEnabled(string taskName, string categoryOrArea)`**: Indica si la tarea está habilitada.

---

## 3. Ejemplos de Uso y Extensibilidad

### A. Definición de un Proveedor de Configuración de Temporizadores (`ITimerConfigProvider`)
```csharp
public class CustomTimerConfigProvider : ITimerConfigProvider
{
    public double GetIntervalMs(string taskName, string categoryOrArea)
    {
        if (taskName == "COLLECTION" && categoryOrArea == "WMI") return 10000; // 10 segundos
        return 30000; // 30 segundos por defecto
    }

    public bool IsEnabled(string taskName, string categoryOrArea) => true;
}
```

### B. Creación y Declaración de un Nuevo Handler de Tarea (`IScheduledTaskHandler`)
Para agregar una nueva tarea programada (ej. recolección WMI), implementa `IScheduledTaskHandler`:

```csharp
public class WmiCollectionTaskHandler : IScheduledTaskHandler
{
    public string TaskName => "COLLECTION";

    public bool CanHandle(string categoryOrArea) => string.Equals(categoryOrArea, "WMI", StringComparison.OrdinalIgnoreCase);

    public async Task<object> ExecuteAsync(string categoryOrArea, CancellationToken cancellationToken)
    {
        // Lógica de recolección WMI...
        await Task.Delay(100, cancellationToken);
        return new { Status = "OK", MemoryUsed = "45%" };
    }
}
```

### C. Instanciación y Registro en el Orquestador
```csharp
// 1. Instanciar los servicios base de Background y Log
var log = new ScreenLog();
var serviceManager = new BackgroundServiceManager(log);
var configProvider = new CustomTimerConfigProvider();

// 2. Componer la lista de handlers disponibles
var handlers = new IScheduledTaskHandler[]
{
    new WmiCollectionTaskHandler()
};

// 3. Crear e iniciar el orquestador
var orchestrator = new ScheduledTaskOrchestrator(serviceManager, configProvider, handlers, log);

// Registrar la tarea y área específica
orchestrator.RegisterTask("COLLECTION", "WMI");

// Iniciar todas las tareas
orchestrator.StartAll();
```
