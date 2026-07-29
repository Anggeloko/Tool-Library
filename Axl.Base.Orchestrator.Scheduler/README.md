# Axl.Base.Orchestrator.Scheduler ⏱️

Library for dynamic scheduled background tasks orchestration following Hexagonal Architecture, Strategy pattern, and Thread-Safe execution.

## Features
- **Hexagonal Ports**: Defines `IScheduledTaskHandler` (Strategy contract) and `ITimerConfigProvider` (dynamic intervals).
- **Background Manager Integration**: Wraps task loops with `Axl.Base.Background` task management.
- **Dynamic Intervals**: Queries `ITimerConfigProvider` for dynamic adjustments without needing service restarts.

## Creating a New Handler

Implement `IScheduledTaskHandler` to define a new scheduled task:

```csharp
public class WmiCollectionTaskHandler : IScheduledTaskHandler
{
    public string TaskName => "COLLECTION";

    public bool CanHandle(string categoryOrArea) => string.Equals(categoryOrArea, "WMI", StringComparison.OrdinalIgnoreCase);

    public async Task<object> ExecuteAsync(string categoryOrArea, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return new { Status = "OK" };
    }
}
```

## Declaration & Usage Example

```csharp
// 1. Declare infrastructure & dependencies
var serviceManager = new BackgroundServiceManager(logger);
var configProvider = new MyTimerConfigProvider();
var handlers = new IScheduledTaskHandler[] { new WmiCollectionTaskHandler() };

// 2. Instantiate Orchestrator
var orchestrator = new ScheduledTaskOrchestrator(serviceManager, configProvider, handlers, logger);

// 3. Register & Start
orchestrator.RegisterTask("COLLECTION", "WMI");
orchestrator.StartAll();
```
