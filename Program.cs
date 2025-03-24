using System;
using System.Collections;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Il2Cpp;
using MelonLoader;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Server;
using UnityEngine;
using YuchiGames.POM.Hooks;
using YuchiGames.POM.Network.Mqtt;


namespace YuchiGames.POM
{
    public class Program : MelonMod
    {
        private IConfiguration _configuration;
        private string worldSeedTopic = "world/seed";
        private string playerTopic = "player/0/transform"; // ユーザ認証を作成した場合には、0の部分に{player_id}が実装されます。
        private bool isPlayerSynchronized = false;
        private bool isPuppet = false; // デバッグ用の無意味変数だよ
        private GameObject player;

        MqttManager mqttManager;
        public override async void OnInitializeMelon()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile($"{Directory.GetCurrentDirectory()}/Mods/config.json", true, false)
                .Build();
            string MQTT_SERVER = _configuration["Mqtt:Server"];
            int MQTT_PORT = int.Parse(_configuration["Mqtt:Port"]);
            string MQTT_USERNAME_A = _configuration["Mqtt:A:Username"];
            string MQTT_PASSWORD_A = _configuration["Mqtt:A:Password"];
            string MQTT_CLIENT_ID_A = _configuration["Mqtt:A:ClientId"];

            mqttManager = new MqttManager(MQTT_SERVER, MQTT_PORT, MQTT_CLIENT_ID_A, MQTT_USERNAME_A, MQTT_PASSWORD_A, true);
            await mqttManager.ConnectAsync();
            WorldLauncher.Instance = new WorldLauncher();
        }

        public override async void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (mqttManager == null)
                {
                    MelonLogger.Error("mqttManager is null!");
                    return;
                }
                int worldSeed = TerrainGenerator.worldSeed;
                await mqttManager.PublishAsync(worldSeedTopic, worldSeed.ToString(), 2, true);
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                await mqttManager.SubscribeAsync(worldSeedTopic, 2, (topic, payload) =>
                {
                    MelonLogger.Msg($"MQTTメッセージ受信: topic={topic}, payload={payload}");
                    MelonCoroutines.Start(WorldLauncher.Instance.ProcessSeedMessageCoroutine(topic, payload));
                });
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (mqttManager == null)
                {
                    MelonLogger.Error("mqttManager is null!");
                    return;
                }
                if (player == null)
                    player = GameObject.Find("Player/XR Origin");
                isPlayerSynchronized = true;
            }
            if (isPlayerSynchronized)
            {
                await mqttManager.PublishAsync(playerTopic, TransformSerializer.TransformToBytes(player.transform), 0, false);
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (mqttManager == null)
                {
                    MelonLogger.Error("mqttManager is null!");
                    return;
                }
                await mqttManager.SubscribeAsync(playerTopic, 0, (topic, payload) =>
                {
                    MelonLogger.Msg($"MQTTメッセージ受信: topic={topic}, payload={BitConverter.ToString(payload)}");
                    MelonCoroutines.Start(UpdatePlayerTransformCoroutine(payload));
                });
            }
        }
        private IEnumerator UpdatePlayerTransformCoroutine(byte[] payload)
        {
            // 1フレーム待つことで確実にメインスレッド上で実行
            yield return null;
            if (player == null)
                    player = GameObject.Find("Player/XR Origin");
            TransformSerializer.BytesToTransform(payload, player.transform);
            MelonLogger.Msg(player.transform.ToString());
        }
    }
}
