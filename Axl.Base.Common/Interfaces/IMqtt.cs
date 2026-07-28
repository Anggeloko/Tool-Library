using System;
using System.Threading.Tasks;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    public interface IMqtt : IDisposable
    {
        event EventHandler<MqttData> DataReceived;
        event EventHandler Connected;
        event EventHandler Disconnected;
        bool IsConnected { get; }
        MqttQos Qos { get; set; }
        bool UseCompression { get; set; }
        Task ConnectAsync();
        Task SubscribeAsync(string[] topics);
        Task PublishAsync(string topic, string payload);
        Task PublishAsync<T>(string topic, T payload);
        Task DisconnectAsync();
    }
}
