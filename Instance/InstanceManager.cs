using System.Text;
using MelonLoader;
using YuchiGames.POM.Hooks;
using YuchiGames.POM.Network.Mqtt;

namespace YuchiGames.POM.Instance
{
    /// <summary>
    /// マルチプレイヤーインスタンスへの参加・退出を管理し、MQTT通信とワールド状態の遷移を処理します。
    /// </summary>
    public class InstanceManager
    {
        /// <summary>
        /// <see cref="InstanceManager"/> のシングルトンインスタンス。
        /// </summary>
        public static InstanceManager? Instance { get; set; }

        public bool IsInInstance => _isInInstance;
        public string PlayerId => _playerId;

        private MqttManager _mqttManager;
        private bool _isInInstance = false;
        private string _playerId = string.Empty;

        public InstanceManager()
        {
            MqttOptions options = Program.MqttOptions
                ?? throw new InvalidOperationException("MqttOptions is not set. Ensure configuration is loaded correctly.");
            MqttManager.Instance = new MqttManager(options);
            _mqttManager = MqttManager.Instance;
            _playerId = options.ClientId;
        }

        /// <summary>
        /// 指定したプレイヤーでマルチプレイヤーインスタンスに参加し、ワールドシードの更新を購読して参加ステータスを送信します。
        /// </summary>
        /// <param name="playerId">インスタンスに参加するプレイヤーのID。</param>
        public async Task EnterInstance()
        {
            if (_isInInstance)
            {
                Melon<Program>.Logger.Warning("Already in an instance. Cannot enter another instance.");
                return;
            }

            _isInInstance = true;

            await _mqttManager.ConnectAsync();

            AvatarManager.Instance = new AvatarManager();

            await _mqttManager.RegisterCallbackAndSubscribeAsync("world/seed", 2, (topic, payload) =>
            {
                string seedText = Encoding.UTF8.GetString(payload);
                MelonCoroutines.Start(WorldLauncher.Instance.ProcessSeedMessageCoroutine(topic, seedText));
            });

            await _mqttManager.PublishAsync($"player/{_playerId}/status", "joined", 2, true);
        }

        /// <summary>
        /// 指定したプレイヤーで現在のマルチプレイヤーインスタンスから退出し、タイトル画面へ遷移して退出ステータスを送信します。
        /// </summary>
        /// <param name="playerId">インスタンスから退出するプレイヤーのID。</param>
        public async Task LeaveInstance()
        {
            if (!_isInInstance)
            {
                Melon<Program>.Logger.Warning("Not currently in an instance. Cannot leave instance.");
                return;
            }

            await _mqttManager.PublishAsync($"player/{_playerId}/status", "left", 2, true);
            await _mqttManager.DisconnectAsync();
            MqttManager.Instance = null;

            MelonCoroutines.Start(WorldLauncher.Instance.BackToTitleCoroutine());

            _isInInstance = false;
        }
    }
}
