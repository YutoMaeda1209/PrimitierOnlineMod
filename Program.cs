using Il2Cpp;
using MelonLoader;
using Microsoft.Extensions.Configuration;
using UnityEngine;
using YuchiGames.POM.Hooks;
using YuchiGames.POM.Instance;
using YuchiGames.POM.Network.Mqtt;
using YuchiGames.POM.Test;


namespace YuchiGames.POM
{
    public class Program : MelonMod
    {
        public static bool IsDebugMsg { get; set; } = false;
        public static MqttOptions? MqttOptions => s_mqttOptions;

        private Dictionary<KeyCode, Func<Task>>? _keyActions;
        private static MqttOptions? s_mqttOptions;

        public override void OnEarlyInitializeMelon()
        {
            _keyActions = new Dictionary<KeyCode, Func<Task>>
            {
                { KeyCode.BackQuote, SwitchDebugMsg },
                { KeyCode.F1, RegisterWorldSeed },
                { KeyCode.F2, EnterInstance },
                { KeyCode.F3, LeaveInstance },
                { KeyCode.F5, GenerateCubeByCubeGenerator },
                { KeyCode.F6, HandleGenerateFromBinaryAsync },
                { KeyCode.F7, GenerateCubeBinaryAsync },
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile($"{Directory.GetCurrentDirectory()}/Mods/config.json", true, false)
                .Build();
            s_mqttOptions = new MqttOptions(
                configuration["Mqtt:Server"],
                int.Parse(configuration["Mqtt:Port"]),
                configuration["Mqtt:YuchiGames:ClientId"],
                configuration["Mqtt:YuchiGames:Username"],
                configuration["Mqtt:YuchiGames:Password"],
                true
            );
        }

        // public override void OnInitializeMelon()
        // {
        //     _configuration = new ConfigurationBuilder()
        //         .AddJsonFile($"{Directory.GetCurrentDirectory()}/Mods/config.json", true, false)
        //         .Build();
        //     MqttOptions = new MqttOptions(
        //         _configuration["Mqtt:Server"] ?? "localhost",
        //         int.Parse(_configuration["Mqtt:Port"] ?? "1883"),
        //         _configuration["Mqtt:YuchiGames_A:ClientId"] ?? "POM_Client",
        //         _configuration["Mqtt:YuchiGames_A:Username"] ?? "username",
        //         _configuration["Mqtt:YuchiGames_A:Password"] ?? "password"
        //     );
        // }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            WorldLauncher.Instance = new WorldLauncher();
        }

        public override async void OnUpdate()
        {
            if (_keyActions == null)
            {
                MelonLogger.Error("keyActions is null");
                return;
            }
            foreach (var entry in _keyActions)
            {
                if (Input.GetKeyDown(entry.Key))
                    await entry.Value();
            }
        }

        public async Task SwitchDebugMsg()
        {
            IsDebugMsg = !IsDebugMsg;
            MelonLogger.Msg($"Debug message is {(IsDebugMsg ? "enabled" : "disabled")}.");
            await Task.CompletedTask;
        }

        public async Task RegisterWorldSeed()
        {
            if (MqttOptions == null)
                throw new NullReferenceException("MqttOptions is null");
            MqttManager? mqttManager = new MqttManager(MqttOptions);
            await mqttManager.ConnectAsync();

            System.Random random = new System.Random();
            string seed = random.Next(int.MinValue, int.MaxValue).ToString();
            await mqttManager.PublishAsync("world/seed", seed, 2, true);

            await mqttManager.DisconnectAsync();
        }

        public async Task EnterInstance()
        {
            InstanceManager.Instance = new InstanceManager();
            await InstanceManager.Instance.EnterInstance();
        }

        public async Task LeaveInstance()
        {
            if (InstanceManager.Instance == null)
                return;
            await InstanceManager.Instance.LeaveInstance();
            InstanceManager.Instance = null;
        }

        // private async Task HandleF1Async()
        // {
        //     int worldSeed = TerrainGenerator.worldSeed;
        //     await mqttManager.PublishAsync(worldSeedTopic, worldSeed.ToString(), 2, true);
        // }

        // private async Task HandleF2Async()
        // {
        //     await mqttManager.RegisterCallbackAndSubscribeAsync(worldSeedTopic, 2, (topic, payload) =>
        //     {
        //         string seedText = Encoding.UTF8.GetString(payload);
        //         MelonLogger.Msg($"Received on {topic}: {seedText}");
        //         MelonCoroutines.Start(WorldLauncher.Instance.ProcessSeedMessageCoroutine(topic, seedText));
        //     });
        // }

        // private async Task HandleF3Async()
        // {
        //     if (player == null)
        //         player = GameObject.Find("Player/XR Origin");
        //     isPlayerSynchronized = true;
        //     await Task.CompletedTask;
        // }

        // private async Task HandleF4Async()
        // {
        //     if (player == null)
        //         player = GameObject.Find("Player/XR Origin");
        //     await mqttManager.RegisterCallbackAndSubscribeAsync(playerTopic, 0, (topic, payload) =>
        //     {
        //         MelonLogger.Msg($"Received on {topic}: {BitConverter.ToString(payload)}");
        //         MelonCoroutines.Start(UpdatePlayerTransformCoroutine(payload));
        //     });
        // }

        private async Task GenerateCubeByCubeGenerator()
        {
            CubeGenerator.GenerateCube(new Vector3(130, 10, 130), new Quaternion(), new Vector3(0.5f, 0.5f, 0.5f), Substance.Stone, CubeAppearance.SectionState.Right, new CubeAppearance().uvOffset, "");
        }

        // private IEnumerator UpdatePlayerTransformCoroutine(byte[] payload)
        // {
        //     // 1フレーム待つことで確実にメインスレッド上で実行
        //     yield return null;
        //     if (player == null)
        //         player = GameObject.Find("Player/XR Origin");
        //     TransformSerializer.BytesToTransform(payload, player.transform);
        //     MelonLogger.Msg(player.transform.ToString());
        // }

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
