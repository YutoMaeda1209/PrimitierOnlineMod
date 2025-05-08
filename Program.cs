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
        public static Program Instance;
        public MqttManager Mqtt => mqttManager;
        private string cubeBaseTopic = "world/cubeBase/{0}"; // {0}にCubeIDが入ります

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
                {KeyCode.F7, GenerateCubeBinaryAsync}
            };
        }

        public override async void OnInitializeMelon()
        {
            try
            {
                Instance = this;
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

                // 接続成功をログに出力
                MelonLogger.Msg("MQTT接続が確立されました");

                WorldLauncher.Instance = new WorldLauncher();
            }
            catch (Exception e)
            {
                MelonLogger.Error($"MQTT初期化中にエラーが発生しました: {e.Message}");
            }
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
            try
            {
                // 接続状態をチェック
                if (mqttManager == null)
                {
                    MelonLogger.Error("MQTTマネージャーが初期化されていません");
                    return;
                }

                if (!mqttManager.IsConnected)
                {
                    MelonLogger.Warning("MQTT接続が切断されています。再接続を試みます...");
                    await mqttManager.ConnectAsync();
                    MelonLogger.Msg("MQTT再接続に成功しました");
                }

                CubeBaseInitializePatch.DisableDefaultGeneration();
                MelonLogger.Msg("Cubeベース初期化フラグを設定しました");

                await mqttManager.RegisterCallbackAndSubscribeAsync("world/+/cubeBase", 2, (topic, payload) =>
                {
                    // メインスレッドで実行するためにコルーチンを使用
                    MelonCoroutines.Start(HandleCubeMessageCoroutine(topic, payload));
                });

                MelonLogger.Msg("Cubeベースのサブスクライブに成功しました");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"F5処理中にエラーが発生しました: {e.Message}");
            }
        }

        private IEnumerator HandleCubeMessageCoroutine(string topic, byte[] payload)
        {
            MelonLogger.Msg($"CubeBaseメッセージ処理開始: {topic}");
            yield return null;
            MelonLogger.Msg("メインスレッドで実行中");

            try
            {
                MelonLogger.Msg("hogehoge");
                if (payload.Length < 44) // 最小ペイロードサイズをチェック
                {
                    MelonLogger.Error($"[MQTT] Invalid payload size ({payload.Length}) on {topic}");
                    yield break;
                }

                // パース
                byte[] posBytes = new byte[12];
                byte[] rotBytes = new byte[16];
                byte[] scaleBytes = new byte[12];
                byte[] subBytes = new byte[4];

                Buffer.BlockCopy(payload, 0, posBytes, 0, 12);
                Buffer.BlockCopy(payload, 12, rotBytes, 0, 16);
                Buffer.BlockCopy(payload, 28, scaleBytes, 0, 12);
                Buffer.BlockCopy(payload, 40, subBytes, 0, 4);

                Vector3 pos = TransformSerializer.BytesToVector3(posBytes);
                Quaternion rot = TransformSerializer.BytesToQuaternion(rotBytes);
                Vector3 scale = TransformSerializer.BytesToVector3(scaleBytes);

                int subInt = BitConverter.ToInt32(subBytes, 0);
                Substance sub = (Substance)subInt;

                // デフォルト設定でCubeBase生成（メインスレッドで実行）
                CubeGenerator.GenerateCube(
                    pos, rot, scale,
                    sub,
                    CubeAppearance.SectionState.Right,
                    new CubeAppearance().uvOffset
                );

                MelonLogger.Msg($"[MQTT] Generated cube '{topic.Split('/')[1]}' with Substance={sub} from {topic}");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"CubeBase生成中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                MelonLogger.Msg("CubeBaseメッセージ処理完了");
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
        /// 40バイトずつパースしてCubeBaseを生成します。
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

                    // CubeBase生成
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

        private async Task HandlePublishCubeBaseBinaryAsync()
        {
            // 生成パラメータ（例として固定値）
            Vector3 pos = new Vector3(130f, 10f, 130f);
            Quaternion rot = Quaternion.Euler(0f, 0f, 0f);
            Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f);
            Substance sub = Substance.Stone;

            // バイト列に変換
            byte[] posBytes = TransformSerializer.Vector3ToBytes(pos);       // 12 bytes
            byte[] rotBytes = TransformSerializer.QuaternionToBytes(rot);    // 16 bytes
            byte[] scaleBytes = TransformSerializer.Vector3ToBytes(scale);     // 12 bytes
            byte[] subBytes = BitConverter.GetBytes((int)sub);               // 4 bytes

            // 全部で 44 バイトのペイロードを組み立て
            byte[] payload = new byte[44];
            Buffer.BlockCopy(posBytes, 0, payload, 0, 12);
            Buffer.BlockCopy(rotBytes, 0, payload, 12, 16);
            Buffer.BlockCopy(scaleBytes, 0, payload, 28, 12);
            Buffer.BlockCopy(subBytes, 0, payload, 40, 4);

            // publish: QOS2, retain=true
            await mqttManager.PublishAsync(cubeBaseTopic, payload, 2, true);
            MelonLogger.Msg($"[MQTT] Published binary CubeBase → {cubeBaseTopic}");
        }
    }
}
