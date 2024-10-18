using MelonLoader;
using UnityEngine;
using YuchiGames.POM.Server.Managers;

namespace YuchiGames.POM.Server
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
            _networkManager = new NetworkManager(_settings.Port);
            MelonEvents.OnUpdate.Subscribe(_networkManager.Update);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            string[] excludeObjects = new string[] { };
            foreach (GameObject obj in allObjects)
            {
                if (!excludeObjects.Contains(obj.name))
                    GameObject.Destroy(obj);
            }

            _networkManager.Start();
        }

        public override void OnApplicationQuit()
        {
            _networkManager.Stop();
        }
    }
}
