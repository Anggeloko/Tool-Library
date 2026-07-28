namespace Axl.Base.Models
{
    /// <summary>
    /// Representa los niveles de Quality of Service (QoS) para MQTT.
    /// </summary>
    public enum MqttQos
    {
        /// <summary>
        /// QoS 0: El mensaje se entrega como mucho una vez (no hay garantía de entrega).
        /// </summary>
        AtMostOnce = 0,

        /// <summary>
        /// QoS 1: El mensaje se entrega al menos una vez (garantiza entrega, pero puede haber duplicados).
        /// </summary>
        AtLeastOnce = 1,

        /// <summary>
        /// QoS 2: El mensaje se entrega exactamente una vez (garantía total sin duplicados).
        /// </summary>
        ExactlyOnce = 2
    }
}
