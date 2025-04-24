using System;
using System.Collections;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
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
    [HarmonyPatch(typeof(CubeGenerator), "OnPlayerChunkChanged")]
    public static class OnPlayerChunkChanged
    {
        [HarmonyPostfix]
        public static void PostfixMethod(CubeGenerator __instance)
        {
            int generationDistance = (int)AccessTools.PropertyGetter(typeof(CubeGenerator), "GenerationDistance").Invoke(CubeGenerator.instance, null);
            Vector2Int playerPosition = CubeGenerator.PlayerChunkPos;
            var grid = CreateSurroundingGrid(playerPosition, generationDistance);

            int size = grid.GetLength(0);
            Debug.Log($"[SurroundingBlocks] grid size: {size}×{size}");
            for (int j = 0; j < size; j++)
            {
                string line = "";
                for (int i = 0; i < size; i++)
                {
                    Vector2Int pos = grid[i, j];
                    line += $"({pos.x,3},{pos.y,3}) ";
                }
                MelonLogger.Msg(line);
            }

            MelonLogger.Msg(CubeGenerator.PlayerChunkPos.ToString() + AccessTools.PropertyGetter(typeof(CubeGenerator), "GenerationDistance").Invoke(CubeGenerator.instance, null));
        }

        public static Vector2Int[,] CreateSurroundingGrid(Vector2Int center, int generationDistance)
        {
            int size = generationDistance * 2 + 1;
            var grid = new Vector2Int[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    // 配列インデックスを相対オフセットに変換し、中心に足し合わせ
                    int x = center.x + (i - generationDistance);
                    int y = center.y + (j - generationDistance);
                    grid[i, j] = new Vector2Int(x, y);
                }
            }

            return grid;
        }
    }
    // [HarmonyPatch(typeof(CubeGenerator), "GenerateTree")]
    // [HarmonyPatch(new[] { typeof(Vector3), typeof(float), typeof(CubeGenerator.TreeType) })]
    // public class GenerateTreePatch
    // {
    //     static void Postfix(Vector3 spaceCenter, float spaceLength, CubeGenerator.TreeType treeType)
    //     {
    //         MelonLogger.Msg($"GenerateTree executed. Position: {spaceCenter}, SpaceLength: {spaceLength}, TreeType: {treeType}");

    //     }
    // }
    public class Program : MelonMod
    {
        private Dictionary<KeyCode, Func<Task>> keyActions;
        private IConfiguration _configuration;
        private string worldSeedTopic = "world/seed";
        private string playerTopic = "player/0/transform"; // ユーザ認証を作成した場合には、0の部分に{player_id}が実装されます。
        private bool isPlayerSynchronized = false;
        private GameObject player;

        MqttManager mqttManager;

        public override void OnEarlyInitializeMelon()
        {
            keyActions = new Dictionary<KeyCode, Func<Task>>
            {
                {KeyCode.F1, HandleF1Async},
                {KeyCode.F2, HandleF2Async},
                {KeyCode.F3, HandleF3Async},
                {KeyCode.F4, HandleF4Async}
            };
        }

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
            if (mqttManager == null)
            {
                MelonLogger.Error("mqttManager is null in OnUpdate!");
                return;
            }
            if (keyActions == null) MelonLogger.Error("keyactions is null");
            foreach (var entry in keyActions)
            {
                if (Input.GetKeyDown(entry.Key))
                    await entry.Value();
            }
            if (isPlayerSynchronized)
            {
                await mqttManager.PublishAsync(playerTopic, TransformSerializer.TransformToBytes(player.transform), 0, false);
            }
        }

        private async Task HandleF1Async()
        {
            int worldSeed = TerrainGenerator.worldSeed;
            await mqttManager.PublishAsync(worldSeedTopic, worldSeed.ToString(), 2, true);
        }

        private async Task HandleF2Async()
        {
            await mqttManager.RegisterCallbackAndSubscribeAsync(worldSeedTopic, 2, (topic, payload) =>
            {
                string seedText = Encoding.UTF8.GetString(payload);
                MelonLogger.Msg($"Received on {topic}: {seedText}");
                MelonCoroutines.Start(WorldLauncher.Instance.ProcessSeedMessageCoroutine(topic, seedText));
            });
        }

        private async Task HandleF3Async()
        {
            if (player == null)
                player = GameObject.Find("Player/XR Origin");
            isPlayerSynchronized = true;
            await Task.CompletedTask;
        }

        private async Task HandleF4Async()
        {
            if (player == null)
                player = GameObject.Find("Player/XR Origin");
            await mqttManager.RegisterCallbackAndSubscribeAsync(playerTopic, 0, (topic, payload) =>
            {
                MelonLogger.Msg($"Received on {topic}: {BitConverter.ToString(payload)}");
                MelonCoroutines.Start(UpdatePlayerTransformCoroutine(payload));
            });
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
