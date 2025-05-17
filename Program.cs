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
using YuchiGames.POM.Test;


namespace YuchiGames.POM
{
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
                {KeyCode.F4, HandleF4Async},
                {KeyCode.F5, HandleF5Async},
                {KeyCode.F6, HandleGenerateFromBinaryAsync},
                {KeyCode.F7, GenerateCubeBinaryAsync},
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

        private async Task HandleF5Async()
        {
            CubeGenerator.GenerateCube(new Vector3(130, 10, 130), new Quaternion(), new Vector3(0.5f, 0.5f, 0.5f), Substance.Stone, CubeAppearance.SectionState.Right, new CubeAppearance().uvOffset, "");
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

        private async Task GenerateCubeBinaryAsync()
        {
            string outPath = Path.Combine(Directory.GetCurrentDirectory(), "Mods", "cubeTransforms.bin");
            RandomCubeBinaryWriter.WriteRandomTransforms(
                outPath,
                count: 16,
                center: new Vector3(130f, 10f, 130f),
                radius: 5f
            );
        }

        /// <summary>
        /// Mods/cubeTransforms.bin を読み込み、
        /// 40バイトずつパースしてキューブを生成します。
        /// </summary>
        private async Task HandleGenerateFromBinaryAsync()
        {
            const int RecordSize = 44;
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Mods", "cubeTransforms.bin");
            if (!File.Exists(path))
            {
                MelonLogger.Error($"バイナリファイルが見つかりません: {path}");
                return;
            }

            byte[] buffer = new byte[RecordSize];
            int count = 0;

            using (FileStream fs = File.OpenRead(path))
            {
                while (fs.Read(buffer, 0, RecordSize) == RecordSize)
                {
                    // position
                    byte[] posBytes = new byte[12];
                    Buffer.BlockCopy(buffer, 0, posBytes, 0, 12);
                    Vector3 pos = TransformSerializer.BytesToVector3(posBytes);

                    // rotation
                    byte[] rotBytes = new byte[16];
                    Buffer.BlockCopy(buffer, 12, rotBytes, 0, 16);
                    Quaternion rot = TransformSerializer.BytesToQuaternion(rotBytes);

                    // scale
                    byte[] sclBytes = new byte[12];
                    Buffer.BlockCopy(buffer, 28, sclBytes, 0, 12);
                    Vector3 scale = TransformSerializer.BytesToVector3(sclBytes);

                    // substance
                    int subInt = BitConverter.ToInt32(buffer, 40);
                    if (!Enum.IsDefined(typeof(Substance), subInt))
                        subInt = (int)Substance.Stone;  // デフォルト
                    Substance sub = (Substance)subInt;

                    // キューブ生成
                    CubeGenerator.GenerateCube(
                        pos,
                        rot,
                        scale,
                        sub,
                        CubeAppearance.SectionState.Right,
                        new CubeAppearance().uvOffset,
                        $"binaryCube_{count}"
                    );

                    count++;
                }

                if (fs.Position % RecordSize != 0)
                    MelonLogger.Warning("バイナリファイル末尾が不正なサイズ。切り捨てられました。");
            }

            MelonLogger.Msg($"Generated {count} cube(s) with Substance from binary.");
            await Task.CompletedTask;
        }
    }
}
