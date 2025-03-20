using MelonLoader;
using System;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Client;
using System.Threading;
using System.Security.Authentication;
using Microsoft.Extensions.Configuration;
using UnityEngine;
using Il2Cpp;
using YuchiGames.POM.Network.Mqtt;
using YuchiGames.POM.Hooks;
using System.Text.Json;
using System.Collections;


namespace YuchiGames.POM
{
    public class Program : MelonMod
    {
        private IConfiguration _configuration;
        private string worldSeedTopic = "world/seed";
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
        }

    }
}
