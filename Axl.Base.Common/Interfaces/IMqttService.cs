using System;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    public interface IMqttService : IDisposable
    {
        event EventHandler<MqttData> DataReceived;
        event EventHandler Connected;
        event EventHandler Disconnected;
        bool IsConnected { get; }
        MqttQos Qos { get; set; }
        bool UseCompression { get; set; }
        void Connect();
        void Subscribe(string[] topics);
        void Publish(string topic, string payload);
        void Publish<T>(string topic, T payload);
        void Disconnect();
    }
}
