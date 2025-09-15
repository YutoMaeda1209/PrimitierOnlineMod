namespace YuchiGames.POM.Network.Mqtt
{
    public class MqttOptions
    {
        public string Server { get; private set; }
        public int Port { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string ClientId { get; private set; }
        public bool UseTls { get; private set; }

        public MqttOptions(string mqttServer, int mqttPort, string mqttClientId, string mqttUsername, string mqttPassword, bool useTls)
        {
            Server = mqttServer;
            Port = mqttPort;
            ClientId = mqttClientId;
            Username = mqttUsername;
            Password = mqttPassword;
            UseTls = useTls;
        }
    }
}
