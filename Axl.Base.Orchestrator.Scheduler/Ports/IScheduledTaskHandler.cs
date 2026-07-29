using System.Threading;
using System.Threading.Tasks;

namespace Axl.Base.Orchestrator.Scheduler.Ports
{
    public interface IScheduledTaskHandler
    {
        string TaskName { get; }
        bool CanHandle(string categoryOrArea);
        Task<object> ExecuteAsync(string categoryOrArea, CancellationToken cancellationToken);
    }
}
