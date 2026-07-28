# Tools.Background

Standardized thread and background task management library for Windows Services and long-running applications.

## Features
- **Multi-Framework**: Compatible with .NET 4.0 and .NET 4.5.2+.
- **Re-entrancy Protection**: Prevents a task from executing over itself if execution takes longer than the timer interval.
- **Async & Sync**: Native support for `Action` and `Func<Task>` delegates.
- **Graceful Shutdown**: Orchestrator that waits for tasks to finish their current cycle before releasing threads.

## Main Components

### BackgroundServiceManager

Acts as the service container and orchestrator.

- **`StartAll()`**: Starts all registered timers.
- **`StopAll(int timeoutMs)`**: Implements the graceful shutdown pattern:
    1. Stops triggering new events.
    2. Enters a polling loop waiting for active threads to clear their `IsRunning` flag.
    3. If the timeout is exceeded (default: 30s), forces closure and logs a warning.

### BackgroundTask

Represents a periodic unit of work.

- **`WorkSync / WorkAsync`**: The delegate containing the business logic.
- **`IntervalProvider`**: Function (usually from `SchedulerUtils`) that defines when it will run next.

## Windows Service Usage Example

```csharp
public partial class MyService : ServiceBase
{
    private BackgroundServiceManager _manager;

    protected override void OnStart(string[] args)
    {
        _manager = new BackgroundServiceManager(_log);

        // Task A: Every 10 minutes
        _manager.AddTask(new BackgroundTask("Acquisitor") {
            WorkAsync = async () => await _engine.DoHeavyWork(),
            IntervalProvider = () => SchedulerUtils.GetNextMinuteInterval(10)
        });

        // Task B: Kill Switch (Restart at 3 AM)
        var killTask = new BackgroundTask("Restart", SchedulerUtils.GetDailyRunAt(3)) {
            WorkSync = () => _manager.InvokeRequestStop()
        };
        _manager.AddTask(killTask);

        // Subscribe to stop request
        _manager.RequestStop += () => this.Stop();

        _manager.StartAll();
    }

    protected override void OnStop()
    {
        _manager.StopAll(30000); // 30s grace period
    }
}
```

## Benefits
1. **Centralized Logs**: Integrates with `ILog` to automatically report task start, completion, and errors.
2. **Exception Handling**: Uses `ExceptionUtils` to unwrap nested errors, simplifying debugging.
3. **Maintainability**: Eliminates hundreds of repetitive lines of timer wrapper boilerplate code in every `Service1.cs`.

---
*Version: 1.1.3*
