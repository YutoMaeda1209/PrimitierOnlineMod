using System;
using System.Collections;
using System.Security.Authentication;
using System.Text;
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
using HarmonyLib;
using MelonLoader.ICSharpCode.SharpZipLib.Zip;




namespace YuchiGames.POM
{
    // [HarmonyPatch(typeof(CubeGenerator), "OnPlayerChunkChanged")]
    // public static class OnPlayerChunkChanged
    // {
    //     [HarmonyPostfix]
    //     public static void PostfixMethod(CubeGenerator __instance)
    //     {
    //         MelonLogger.Msg(CubeGenerator.PlayerChunkPos.ToString() + AccessTools.PropertyGetter(typeof(CubeGenerator), "GenerationDistance").Invoke(CubeGenerator.instance, null));
    //     }
    // }
    // [HarmonyPatch(typeof(CubeGenerator), "GenerateTree")]
    // [HarmonyPatch(new[] { typeof(Vector3), typeof(float), typeof(CubeGenerator.TreeType) })]
    // public class GenerateTreePatch
    // {
    //     static void Postfix(Vector3 spaceCenter, float spaceLength, CubeGenerator.TreeType treeType)
    //     {
    //         MelonLogger.Msg($"GenerateTree executed. Position: {spaceCenter}, SpaceLength: {spaceLength}, TreeType: {treeType}");

    //     }
    // }

    [HarmonyPatch(typeof(CubeBase), "Initialize")]
    public class CubeBaseInitializePatch
    {
        static void Postfix(CubeBase __instance)
        {
            if (__instance.gameObject.GetComponent<CubeIDHolder>() != null)
                return;

            Vector3 pos = __instance.transform.position;
            Vector2Int chunk = CubeGenerator.WorldToChunkPos(pos);

            string cubeID = CubeBaseIDGenerator.GenerateID(chunk, "Loaded");

            CubeIDHolder idHolder = __instance.gameObject.AddComponent<CubeIDHolder>();
            idHolder.CubeID = cubeID;
            MelonLogger.Msg($"(Loaded) Assigned CubeBase ID: {cubeID}");
        }
    }
    [RegisterTypeInIl2Cpp]
    public class CubeIDHolder : MonoBehaviour
    {
        public string CubeID;
        public CubeIDHolder(IntPtr ptr) : base(ptr) {}
    }
    public static class CubeBaseIDGenerator
    {
        private static Dictionary<string, int> counters = new Dictionary<string, int>();
        public static string GenerateID(Vector2Int chunk, string tag)
        {
            string key = $"{chunk.x}_{chunk.y}_{tag}";
            if (!counters.ContainsKey(key))
            {
                counters[key] = 0;
            }
            counters[key]++;
            return $"{chunk.x}_{chunk.y}_{tag}_{counters[key]}";
        }
    }
    [HarmonyPatch(typeof(CubeGenerator), "GenerateCube",
    new[] { typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Substance), typeof(CubeAppearance.SectionState), typeof(CubeAppearance.UVOffset), typeof(string) })]
    public class GenerateCubePatch_WithRotation
    {
        static void Postfix(Vector3 pos, Quaternion rot, Vector3 size, Substance substance,
                              CubeAppearance.SectionState sectionState, CubeAppearance.UVOffset uvOffset,
                              string tag, CubeBase __result)
        {
            // チャンク座標を取得
            Vector2Int temp = CubeGenerator.WorldToChunkPos(pos);
            MelonLogger.Msg($"GeneratedAt: {pos} | Chunk: {temp.x} : {temp.y}");

            // CubeBase に固有のIDを割り当てる
            string cubeBaseID = CubeBaseIDGenerator.GenerateID(temp, tag);
            CubeIDHolder idHolder = __result.gameObject.AddComponent<CubeIDHolder>();
            idHolder.CubeID = cubeBaseID;
            MelonLogger.Msg($"Assigned CubeBase ID: {cubeBaseID}");

            // MQTT 送信用トピック例（後続処理として）
            string topic = $"chunks/{temp.x}/{temp.y}/cubebase/";
            byte[] payload = Encoding.UTF8.GetBytes(pos.ToString());
            if (Program.Instance != null && Program.Instance.mqttManager != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        //await Program.Instance.mqttManager.PublishAsync(topic, payload, 2, true);
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error publishing to topic {topic}: {ex}");
                    }
                });
            }
        }
    }
    [HarmonyPatch(typeof(CubeGenerator), "GenerateCube",
        new[] { typeof(Vector3), typeof(Vector3), typeof(Substance), typeof(CubeAppearance.SectionState), typeof(CubeAppearance.UVOffset), typeof(string) })]
    public class GenerateCubePatch_WithoutRotation
    {
        static void Postfix(Vector3 pos, Vector3 size, Substance substance,
                              CubeAppearance.SectionState sectionState, CubeAppearance.UVOffset uvOffset,
                              string tag, CubeBase __result)
        {
            Vector2Int temp = CubeGenerator.WorldToChunkPos(pos);
            MelonLogger.Msg($"GeneratedAt: {pos} | Chunk: {temp.x} : {temp.y}");

            // CubeBase に固有のIDを割り当てる
            string cubeBaseID = CubeBaseIDGenerator.GenerateID(temp, tag);
            CubeIDHolder idHolder = __result.gameObject.AddComponent<CubeIDHolder>();
            idHolder.CubeID = cubeBaseID;
            MelonLogger.Msg($"Assigned CubeBase ID: {cubeBaseID}");

            // MQTT 送信用トピック例
            string topic = $"chunks/{temp.x}/{temp.y}";
            byte[] payload = Encoding.UTF8.GetBytes(pos.ToString());
            if (Program.Instance != null && Program.Instance.mqttManager != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        //await Program.Instance.mqttManager.PublishAsync(topic, payload, 2, true);
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error publishing to topic {topic}: {ex}");
                    }
                });
            }
        }
    }
    public class Program : MelonMod
    {
        public static Program Instance {get; private set;}
        private Dictionary<KeyCode, Func<Task>> keyActions;
        private IConfiguration _configuration;
        private string worldSeedTopic = "world/seed";
        private string playerTopic = "player/0/transform"; // ユーザ認証を作成した場合には、0の部分に{player_id}が実装されます。
        private bool isPlayerSynchronized = false;
        private GameObject player;

        public MqttManager mqttManager {get; private set;}

        public override void OnEarlyInitializeMelon()
        {
            Instance = this;
            keyActions = new Dictionary<KeyCode, Func<Task>>
            {
                {KeyCode.F1, HandleF1Async},
                {KeyCode.F2, HandleF2Async},
                {KeyCode.F3, HandleF3Async},
                {KeyCode.F4, HandleF4Async},
                {KeyCode.F5, HandleF5Async}
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

        private Task HandleF5Async()
        {
            Vector2Int chunk = CubeGenerator.PlayerChunkPos;
            MelonLogger.Msg(chunk.ToString());
            return Task.CompletedTask;
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
