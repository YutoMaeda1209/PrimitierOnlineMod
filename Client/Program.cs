using MelonLoader;
using YuchiGames.POM.Client.Managers;

namespace YuchiGames.POM.Client
{
    public class Program : MelonMod
    {
        public Settings Settings => _settings
            ?? throw new NullReferenceException("Settings have not been initialized.");

        private Settings? _settings;
        private NetworkManager _networkManager = new NetworkManager();

        public override void OnInitializeMelon()
        {
            _settings = new Settings();
            _networkManager = new NetworkManager(_settings.IPAddress, _settings.Port);
            MelonEvents.OnUpdate.Subscribe(_networkManager.Update);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _networkManager.Connect();
        }

        public override void OnApplicationQuit()
        {
            _networkManager.Disconnect();
        }
    }
}