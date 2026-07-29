using System.Threading;
using System.Threading.Tasks;
using Axl.Base.Models;

namespace Axl.Base.Orchestrator.Mqtt.Ports
{
    public interface IMqttTopicHandler
    {
        string SubscriptionTopic { get; }
        bool CanHandle(string topic);
        Task HandleAsync(string topic, MqttData data, CancellationToken cancellationToken);
    }
}
