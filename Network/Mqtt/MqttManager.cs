using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Internal;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace YuchiGames.POM.Network.Mqtt
{
    public class MqttManager
    {
        private readonly string _server;
        private readonly int _port;
        private readonly string _clientId;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _useTls;
        private readonly IMqttClient _mqttClient;

        /// <summary>
        /// コンストラクタで接続情報を設定します。
        /// </summary>
        /// <param name="server">MQTTブローカーのアドレス</param>
        /// <param name="port">ポート番号</param>
        /// <param name="clientId">クライアントID</param>
        /// <param name="username">認証用ユーザー名（任意）</param>
        /// <param name="password">認証用パスワード（任意）</param>
        /// <param name="useTls">TLS接続を使用するかどうか</param>
        public MqttManager(string server, int port, string clientId, string username = null, string password = null, bool useTls = false)
        {
            _server = server;
            _port = port;
            _clientId = clientId;
            _username = username;
            _password = password;
            _useTls = useTls;
            _mqttClient = new MqttFactory().CreateMqttClient();
        }

        /// <summary>
        /// MQTTブローカーへの接続を行います。
        /// </summary>
        public async Task ConnectAsync()
        {
            MqttClientOptionsBuilder optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_server, _port)
                .WithClientId(_clientId)
                .WithProtocolVersion(MqttProtocolVersion.V500);

            if (!string.IsNullOrEmpty(_username) && _password != null)
            {
                optionsBuilder = optionsBuilder.WithCredentials(_username, _password);
            }

            if (_useTls)
            {
                optionsBuilder = optionsBuilder.WithTlsOptions(o =>
                {
                    o.WithCertificateValidationHandler(_ => true);
                    o.WithSslProtocols(SslProtocols.Tls12);
                });
            }

            MqttClientOptions options = optionsBuilder.Build();
            await _mqttClient.ConnectAsync(options, CancellationToken.None);
            Melon<Program>.Logger.Msg("Connected to MQTT broker.");
        }

        /// <summary>
        /// Publish処理（topicとpayloadのみ指定、QoS=0、retain=false）。
        /// </summary>
        public async Task PublishAsync(string topic, string payload)
        {
            await PublishAsync(topic, payload, qos: 0, retain: false);
        }

        /// <summary>
        /// Publish処理（topic, payload, QoSを指定、retain=false）。
        /// </summary>
        public async Task PublishAsync(string topic, string payload, int qos)
        {
            await PublishAsync(topic, payload, qos, retain: false);
        }

        /// <summary>
        /// Publish処理（topic, payload, QoS, retainフラグを指定）。
        /// </summary>
        public async Task PublishAsync(string topic, string payload, int qos, bool retain)
        {
            if (!_mqttClient.IsConnected)
            {
                throw new InvalidOperationException("MQTT client is not connected. Call ConnectAsync() first.");
            }

            MqttQualityOfServiceLevel quality;
            switch (qos)
            {
                case 0:
                    quality = MqttQualityOfServiceLevel.AtMostOnce;
                    break;
                case 1:
                    quality = MqttQualityOfServiceLevel.AtLeastOnce;
                    break;
                case 2:
                    quality = MqttQualityOfServiceLevel.ExactlyOnce;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(qos), "QoS must be 0, 1, or 2.");
            }

            MqttApplicationMessage message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(quality)
                .WithRetainFlag(retain)
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
            Melon<Program>.Logger.Msg($"Published message to topic '{topic}' with QoS {qos} and retain flag {retain}.");
        }

        public async Task PublishAsync(string topic, byte[] payload, int qos, bool retain)
        {
            if (!_mqttClient.IsConnected)
            {
                throw new InvalidOperationException("MQTT client is not connected. Call ConnectAsync() first.");
            }

            MqttQualityOfServiceLevel quality;
            switch (qos)
            {
                case 0:
                    quality = MqttQualityOfServiceLevel.AtMostOnce;
                    break;
                case 1:
                    quality = MqttQualityOfServiceLevel.AtLeastOnce;
                    break;
                case 2:
                    quality = MqttQualityOfServiceLevel.ExactlyOnce;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(qos), "QoS must be 0, 1, or 2.");
            }

            MqttApplicationMessage message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(quality)
                .WithRetainFlag(retain)
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
            Melon<Program>.Logger.Msg($"Published message to topic '{topic}' with QoS {qos} and retain flag {retain}.");
        }

        /// <summary>
        /// 指定したtopicに対してSubscribeを行います。メッセージ受信時のコールバックを指定可能です。
        /// </summary>
        /// <param name="topic">購読するトピック</param>
        /// <param name="qos">QoSレベル（0～2）</param>
        /// <param name="messageReceivedCallback">メッセージ受信時に呼び出されるコールバック（topic, payload）</param>
        public async Task SubscribeAsync(string topic, int qos = 0, Action<string, string> messageReceivedCallback = null)
        {
            if (!_mqttClient.IsConnected)
            {
                throw new InvalidOperationException("MQTT client is not connected. Call ConnectAsync() first.");
            }

            MqttQualityOfServiceLevel quality;
            switch (qos)
            {
                case 0:
                    quality = MqttQualityOfServiceLevel.AtMostOnce;
                    break;
                case 1:
                    quality = MqttQualityOfServiceLevel.AtLeastOnce;
                    break;
                case 2:
                    quality = MqttQualityOfServiceLevel.ExactlyOnce;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(qos), "QoS must be 0, 1, or 2.");
            }

            MqttTopicFilter topicFilter = new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(quality)
                .Build();

            MqttClientSubscribeOptions subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topicFilter)
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                string receivedTopic = e.ApplicationMessage.Topic;
                string receivedPayload = e.ApplicationMessage.ConvertPayloadToString();
                messageReceivedCallback?.Invoke(receivedTopic, receivedPayload);
                return Task.CompletedTask;
            };

            await _mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None);
            Melon<Program>.Logger.Msg($"Subscribed to topic '{topic}' with QoS {qos}.");
        }

        /// <summary>
        /// 指定したtopicに対してSubscribeを行います。メッセージ受信時のコールバック（topic, byte[] payload）を指定可能です。
        /// </summary>
        /// <param name="topic">購読するトピック</param>
        /// <param name="qos">QoSレベル（0～2）</param>
        /// <param name="messageReceivedCallback">メッセージ受信時に呼び出されるコールバック（topic, byte[] payload）</param>
        public async Task SubscribeAsync(string topic, int qos = 0, Action<string, byte[]> messageReceivedCallback = null)
        {
            if (!_mqttClient.IsConnected)
            {
                throw new InvalidOperationException("MQTT client is not connected. Call ConnectAsync() first.");
            }

            MqttQualityOfServiceLevel quality;
            switch (qos)
            {
                case 0:
                    quality = MqttQualityOfServiceLevel.AtMostOnce;
                    break;
                case 1:
                    quality = MqttQualityOfServiceLevel.AtLeastOnce;
                    break;
                case 2:
                    quality = MqttQualityOfServiceLevel.ExactlyOnce;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(qos), "QoS must be 0, 1, or 2.");
            }

            MqttTopicFilter topicFilter = new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(quality)
                .Build();

            MqttClientSubscribeOptions subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topicFilter)
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                string receivedTopic = e.ApplicationMessage.Topic;
                byte[] receivedPayload = new byte[e.ApplicationMessage.PayloadSegment.Count];
                Array.Copy(e.ApplicationMessage.PayloadSegment.Array, e.ApplicationMessage.PayloadSegment.Offset, receivedPayload, 0, e.ApplicationMessage.PayloadSegment.Count); messageReceivedCallback?.Invoke(receivedTopic, receivedPayload);
                MelonLogger.Msg(BitConverter.ToString(receivedPayload));
                return Task.CompletedTask;
            };

            await _mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None);
            Melon<Program>.Logger.Msg($"Subscribed to topic '{topic}' with QoS {qos}.");
        }


        /// <summary>
        /// MQTTブローカーから切断します。
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
                Melon<Program>.Logger.Msg("Disconnected from MQTT broker.");
            }
        }
    }
}
