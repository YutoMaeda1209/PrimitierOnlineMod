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
using System.Text.Json;


namespace YuchiGames.POM
{
    public class Program : MelonMod
    {
        private IConfiguration _configuration;
        private string TOPIC = "world/seed";
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
                await mqttManager.PublishAsync(TOPIC, worldSeed.ToString(), 2, true);
            }
        }

    }
}
