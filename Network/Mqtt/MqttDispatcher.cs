using MQTTnet.Client;

namespace YuchiGames.POM.Network.Mqtt
{
    public class MqttDispatcher
    {
        private readonly Dictionary<string, Action<string, byte[]>> _topicCallbacks = new Dictionary<string, Action<string, byte[]>>();

        public void Register(string topic, Action<string, byte[]> callback)
        {
            _topicCallbacks[topic] = callback;
        }

        public Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            string receivedTopic = e.ApplicationMessage.Topic;
            byte[] payload = new byte[e.ApplicationMessage.PayloadSegment.Count];
            Array.Copy(e.ApplicationMessage.PayloadSegment.Array, e.ApplicationMessage.PayloadSegment.Offset, payload, 0, e.ApplicationMessage.PayloadSegment.Count);
            if (_topicCallbacks.TryGetValue(receivedTopic, out var callback))
                callback(receivedTopic, payload);
            return Task.CompletedTask;
        }

    }
}