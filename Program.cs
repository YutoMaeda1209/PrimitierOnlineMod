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
using System.Collections.Concurrent;
using YuchiGames.POM.Protocol;
using Il2Cpp;

namespace YuchiGames.POM
{
    public class Program : MelonMod
    {
        private Dictionary<KeyCode, Func<Task>> keyActions;
        private IConfiguration _configuration;
        private bool isPlayerSynchronized = false;
        private GameObject player;
        public static Program Instance;
        public MqttManager Mqtt => mqttManager;
        private int worldSeed = 0;

        MqttManager mqttManager;

        private bool isWorldSeedCallbackRegistered = false;
        private ConcurrentQueue<(string topic, byte[] payload)> cubeMessageQueue = new ConcurrentQueue<(string, byte[])>();
        private const int MaxMessagesPerFrame = 5; // 負荷低減用、デフォは5

        public static bool isHost = true;
        public static readonly object _lockObject = new object();
        public static bool _isInternalGeneration = false;
        private static HashSet<string> _generatedCubeIds = new HashSet<string>();
        public override void OnEarlyInitializeMelon()
        {
            keyActions = new Dictionary<KeyCode, Func<Task>>
            {
                {KeyCode.F1, HandleF1Async},
                {KeyCode.F2, HandleF2Async},
                {KeyCode.F3, HandleF3Async},
                {KeyCode.F4, HandleF4Async},
                {KeyCode.F5, HandleF5Async},
                {KeyCode.F6, HandlePublishCubeBaseBinaryAsync},
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
                string MQTT_USERNAME_A = _configuration["Mqtt:kirimine170_A:Username"];
                string MQTT_PASSWORD_A = _configuration["Mqtt:kirimine170_A:Password"];
                string MQTT_CLIENT_ID_A = _configuration["Mqtt:kirimine170_A:ClientId"];

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
                await mqttManager.PublishAsync(Topic.PlayerTransform(0), TransformCodec.TransformToBytes(player.transform), 0, false);
            }

            // queueから処理
            for (int i = 0; i < MaxMessagesPerFrame; i++)
            {
                if (cubeMessageQueue.TryDequeue(out var message))
                {
                    MelonCoroutines.Start(HandleCubeMessageCoroutine(message.topic, message.payload));
                }
                else
                {
                    break; // queueが空
                }
            }
        }

        private async Task HandleF1Async()
        {
            int worldSeed = TerrainGenerator.worldSeed;
            await mqttManager.PublishAsync(Topic.WorldSeed, worldSeed.ToString(), 2, true);
        }

        private async Task HandleF2Async()
        {
            isHost = false;
            if (worldSeed != 0)
            {
                // worldSeedが格納されている場合、その値を使ってWorldLauncherを起動
                MelonLogger.Msg($"Using stored worldSeed: {worldSeed}");
                MelonCoroutines.Start(WorldLauncher.Instance.ProcessSeedMessageCoroutine(Topic.WorldSeed, worldSeed.ToString()));
            }
            else if (!isWorldSeedCallbackRegistered)
            {
                // worldSeedが格納されていない場合、コールバックを登録
                await mqttManager.RegisterCallbackAndSubscribeAsync(Topic.WorldSeed, 2, (topic, payload) =>
                {
                    string seedText = Encoding.UTF8.GetString(payload);
                    if (int.TryParse(seedText, out int seed))
                    {
                        worldSeed = seed;
                        MelonLogger.Msg($"Received and stored worldSeed: {worldSeed}");
                        MelonCoroutines.Start(WorldLauncher.Instance.ProcessSeedMessageCoroutine(topic, seedText));
                    }
                    else
                    {
                        MelonLogger.Error($"Invalid worldSeed received: {seedText}");
                    }
                });
                isWorldSeedCallbackRegistered = true;
            }
            else
            {
                MelonLogger.Msg("World seed callback is already registered.");
            }
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
            await mqttManager.RegisterCallbackAndSubscribeAsync(Topic.PlayerTransform(0), 0, (topic, payload) =>
            {
                MelonLogger.Msg($"Received on {topic}: {BitConverter.ToString(payload)}");
                MelonCoroutines.Start(UpdatePlayerTransformCoroutine(payload));
            });
        }

        private async Task HandleF5Async()
        {
            try
            {
                if (mqttManager == null)
                {
                    MelonLogger.Error("MQTTマネージャーが初期化されていません");
                    return;
                }
                if (!mqttManager.IsConnected)
                {
                    MelonLogger.Warning("MQTT接続が切断、再接続...");
                    await mqttManager.ConnectAsync();
                    MelonLogger.Msg("MQTT再接続完了");
                }

                await mqttManager.RegisterCallbackAndSubscribeAsync(Topic.CubeBaseFilter, 2, (topic, payload) =>
                {
                    cubeMessageQueue.Enqueue((topic, payload));
                });

                MelonLogger.Msg("CubeBaseサブスクライブ完了");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"F5処理中にエラー: {e.Message}");
            }
        }

        private IEnumerator HandleCubeMessageCoroutine(string topic, byte[] payload)
        {
            yield return null; // メインスレッド

            try
            {
                if (payload == null || payload.Length != Wire.CubeBaseSize)
                {
                    MelonLogger.Error($"[MQTT] Invalid payload size {payload?.Length} on {topic}, expected {Wire.CubeBaseSize}");
                    yield break;
                }

                CubeBaseCodec.Parse(payload,
                    out var pos, out var rot, out var scale,
                    out var anchor, out var substance, out var flags);

                                // 内部生成フラグを設定して無限ループを防ぐ
                lock (_lockObject)
                {
                    _isInternalGeneration = true;
                }

                CubeBase cb = CubeGenerator.GenerateCube(
                    pos, rot, scale,
                    substance,
                    CubeAppearance.SectionState.Right,
                    new CubeAppearance().uvOffset,
                    "pom"
                );

                // 内部生成フラグを元に戻す
                lock (_lockObject)
                {
                    _isInternalGeneration = false;
                }

                // null チェックを追加
                if (cb == null)
                {
                    MelonLogger.Error($"CubeGenerator.GenerateCube returned null for topic: {topic}");
                    yield break;
                }

                cb.name = $"pom:{cb.name}";
                cb.tag  = "pom";

                var connector = cb.GetComponent<CubeConnector>();
                if (connector != null)
                {
                    connector.anchor = anchor;
                }
                else
                {
                    MelonLogger.Warning($"CubeConnector not found for generated cube: {cb.name}");
                }

                string cubeId = topic[(topic.LastIndexOf('/') + 1)..];

                // 重複チェック
                lock (_lockObject)
                {
                    if (_generatedCubeIds.Contains(cubeId))
                    {
                        MelonLogger.Warning($"[MQTT] Cube '{cubeId}' already exists, skipping generation");
                        yield break;
                    }
                    _generatedCubeIds.Add(cubeId);
                }

                MelonLogger.Msg($"[MQTT] Generated cube '{cubeId}' Anchor={anchor}, Substance={substance}, Flags={flags}");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"CubeBase生成中にエラー: {e.Message}\n{e.StackTrace}");
            }
        }

        private IEnumerator UpdatePlayerTransformCoroutine(byte[] payload)
        {
            // 1フレーム待つことで確実にメインスレッド上で実行
            yield return null;
            if (player == null)
                player = GameObject.Find("Player/XR Origin");
            TransformCodec.BytesToTransform(payload, player.transform);
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
            // CubeBaseInitializePatch.IsInternalGenerate = true;
            const int RecordSize = 44;
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Mods", "cubeTransforms.bin");
            if (!File.Exists(path))
            {
                MelonLogger.Error($"バイナリファイルが見つかりません: {path}");
                return;
            }

            byte[] buffer = new byte[RecordSize];
            int count = 0;

            using (var fs = File.OpenRead(path))
            {
                while (fs.Read(buffer, 0, RecordSize) == RecordSize)
                {
                    // position
                    var posBytes = new byte[12];
                    Buffer.BlockCopy(buffer, 0, posBytes, 0, 12);
                    Vector3 pos = TransformCodec.BytesToVector3(posBytes);

                    // rotation
                    var rotBytes = new byte[16];
                    Buffer.BlockCopy(buffer, 12, rotBytes, 0, 16);
                    Quaternion rot = TransformCodec.BytesToQuaternion(rotBytes);

                    // scale
                    var sclBytes = new byte[12];
                    Buffer.BlockCopy(buffer, 28, sclBytes, 0, 12);
                    Vector3 scale = TransformCodec.BytesToVector3(sclBytes);

                    // substance
                    int subInt = BitConverter.ToInt32(buffer, 40);
                    if (!Enum.IsDefined(typeof(Substance), subInt))
                        subInt = (int)Substance.Stone;
                    Substance sub = (Substance)subInt;

                                        // 内部生成フラグを設定して無限ループを防ぐ
                    lock (_lockObject)
                    {
                        _isInternalGeneration = true;
                    }

                    CubeBase cb = CubeGenerator.GenerateCube(
                        pos,
                        rot,
                        scale,
                        sub,
                        CubeAppearance.SectionState.Right,
                        new CubeAppearance().uvOffset,
                        "pom"
                    );

                    // 内部生成フラグを元に戻す
                    lock (_lockObject)
                    {
                        _isInternalGeneration = false;
                    }

                    // null チェックを追加
                    if (cb == null)
                    {
                        MelonLogger.Error($"CubeGenerator.GenerateCube returned null in binary generation");
                        continue;
                    }

                    cb.name = $"pom{cb.name}";
                    cb.tag = "pom";

                    count++;
                }

                if (fs.Position % RecordSize != 0)
                    MelonLogger.Warning("バイナリファイル末尾が不正なサイズ。切り捨てられました。");
            }

            MelonLogger.Msg($"Generated {count} cube(s) with Substance from binary.");
            // CubeBaseInitializePatch.IsInternalGenerate = false;
            await Task.CompletedTask;
        }

        private async Task HandlePublishCubeBaseBinaryAsync()
        {
            // 例: 実運用では適切に cubeId/値を取得
            int cubeId = 123;
            string topic = Topic.CubeBase(cubeId.ToString());

            Vector3 pos   = new Vector3(130f, 10f, 130f);
            Quaternion rot= Quaternion.Euler(0f, 0f, 0f);
            Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f);
            var anchor    = CubeConnector.Anchor.Permanent; // TODO 取得する方針に
            var sub       = Substance.Stone;
            byte flags    = 0; // NOTE デバッグ用

            byte[] payload = CubeBaseCodec.Compose(pos, rot, scale, anchor, sub, flags);
            await mqttManager.PublishAsync(topic, payload, 2, true);

            MelonLogger.Msg($"[MQTT] Published CubeBase(45B) → {topic}");
        }
    }
}
