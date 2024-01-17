using MelonLoader;
using UnityEngine;

namespace MirrorLibTest
{
    public class Class1: MelonMod
    {
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene {sceneName} with build index {buildIndex} has been loaded!");
            var obj = new GameObject("NetworkingManager");
            // obj.AddComponent<NetworkManager>();
        }
    }
}
