using System;
using System.Collections.Generic;
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
        private IMqttClient _mqttClient;
        private MqttClientOptions _options;
        private Dictionary<string, Action<string, byte[]>> _topicCallbacks;
        private bool _isConnecting;
        private readonly object _connectionLock = new object();

        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        /// <summary>
        /// コンストラクタで接続情報を設定するよ
        /// </summary>
        /// <param name="server">MQTTブローカーのアドレス</param>
        /// <param name="port">ポート番号</param>
        /// <param name="clientId">クライアントID</param>
        /// <param name="username">認証用ユーザー名</param>
        /// <param name="password">認証用パスワード</param>
        /// <param name="useTls">TLS接続</param>
        public MqttManager(string server, int port, string clientId, string username, string password, bool useTls)
        {
            _topicCallbacks = new Dictionary<string, Action<string, byte[]>>();
            InitializeMqttClient(server, port, clientId, username, password, useTls);
        }

        private void InitializeMqttClient(string server, int port, string clientId, string username, string password, bool useTls)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(server, port)
                .WithClientId(clientId)
                .WithCredentials(username, password);

            if (useTls)
            {
                optionsBuilder.WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12
                });
            }

            _options = optionsBuilder.Build();

            _mqttClient.DisconnectedAsync += async e =>
            {
                MelonLogger.Warning("MQTT接続が切断されました");
                await HandleReconnection();
            };

            // メッセージ受信時のハンドラを設定
            _mqttClient.ApplicationMessageReceivedAsync += HandleMessageReceived;
        }

        private async Task HandleReconnection()
        {
            try
            {
                await ConnectAsync();
                // 再接続後、既存のサブスクリプションを再購読
                foreach (var callback in _topicCallbacks)
                {
                    await SubscribeToTopicAsync(callback.Key, 2);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"再接続中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// MQTTブローカーへの接続を行います。
        /// </summary>
        public async Task ConnectAsync()
        {
            lock (_connectionLock)
            {
                if (_isConnecting)
                {
                    MelonLogger.Warning("接続処理が既に実行中です");
                    return;
                }
                _isConnecting = true;
            }

            try
            {
                if (_mqttClient.IsConnected)
                {
                    MelonLogger.Msg("既に接続済みです");
                    return;
                }

                await _mqttClient.ConnectAsync(_options);
                MelonLogger.Msg("MQTT接続が確立されました");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"MQTT接続中にエラーが発生しました: {ex.Message}");
                throw;
            }
            finally
            {
                lock (_connectionLock)
                {
                    _isConnecting = false;
                }
            }
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

        public void RegisterCallback(string topic, Action<string, byte[]> callback)
        {
            _topicCallbacks[topic] = callback;
        }

        public async Task RegisterCallbackAndSubscribeAsync(string topic, int qos, Action<string, byte[]> callback)
        {
            try
            {
                if (!IsConnected)
                {
                    MelonLogger.Warning("接続が切断されています。再接続を試みます...");
                    await ConnectAsync();
                }

                // コールバックを登録
                _topicCallbacks[topic] = callback;
                MelonLogger.Msg($"コールバックを登録しました: {topic}");

                // トピックをサブスクライブ
                await SubscribeToTopicAsync(topic, qos);
                MelonLogger.Msg($"トピック {topic} のサブスクライブに成功しました");

                // 現在登録されているコールバックの一覧を表示（デバッグ用）
                MelonLogger.Msg("現在登録されているコールバック:");
                foreach (var kvp in _topicCallbacks)
                {
                    MelonLogger.Msg($"- {kvp.Key}");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"サブスクライブ中にエラーが発生しました: {e.Message}");
                throw;
            }
        }

        private async Task SubscribeToTopicAsync(string topic, int qos)
        {
            var mqttSubscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos))
                .Build();

            await _mqttClient.SubscribeAsync(mqttSubscribeOptions);
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

        private Task HandleMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string receivedTopic = e.ApplicationMessage.Topic;
                MelonLogger.Msg($"メッセージを受信しました: {receivedTopic}");

                byte[] receivedPayload = new byte[e.ApplicationMessage.PayloadSegment.Count];
                Array.Copy(e.ApplicationMessage.PayloadSegment.Array,
                          e.ApplicationMessage.PayloadSegment.Offset,
                          receivedPayload,
                          0,
                          e.ApplicationMessage.PayloadSegment.Count);

                // 完全一致するトピックをチェック
                if (_topicCallbacks.TryGetValue(receivedTopic, out var callback))
                {
                    MelonLogger.Msg($"完全一致するコールバックを実行します: {receivedTopic}");
                    callback?.Invoke(receivedTopic, receivedPayload);
                }
                else
                {
                    // ワイルドカードとのマッチング
                    bool matched = false;
                    foreach (var kvp in _topicCallbacks)
                    {
                        if (IsTopicMatch(kvp.Key, receivedTopic))
                        {
                            MelonLogger.Msg($"ワイルドカードでコールバックを実行します: {kvp.Key} -> {receivedTopic}");
                            kvp.Value?.Invoke(receivedTopic, receivedPayload);
                            matched = true;
                        }
                    }

                    if (!matched)
                    {
                        MelonLogger.Warning($"トピック '{receivedTopic}' に一致するコールバックが見つかりませんでした");
                    }
                }

                MelonLogger.Msg($"受信トピック: {receivedTopic}");
                MelonLogger.Msg($"登録されているトピックパターン:");
                foreach (var pattern in _topicCallbacks.Keys)
                {
                    MelonLogger.Msg($"- {pattern} (マッチ: {IsTopicMatch(pattern, receivedTopic)})");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"メッセージ処理中にエラーが発生しました: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private bool IsTopicMatch(string pattern, string topic)
        {
            // シンプルなワイルドカードマッチング
            // TODO #のワイルドカードマッチング作ってないよ
            if (pattern == topic) return true;

            var patternParts = pattern.Split('/');
            var topicParts = topic.Split('/');

            if (patternParts.Length != topicParts.Length) return false;

            for (int i = 0; i < patternParts.Length; i++)
            {
                if (patternParts[i] == "+") continue;
                if (patternParts[i] != topicParts[i]) return false;
            }

            return true;
        }
    }
}
