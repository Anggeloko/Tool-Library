namespace Axl.Base.Orchestrator.Scheduler.Ports
{
    public interface ITimerConfigProvider
    {
        double GetIntervalMs(string taskName, string categoryOrArea);
        bool IsEnabled(string taskName, string categoryOrArea);
    }
}
