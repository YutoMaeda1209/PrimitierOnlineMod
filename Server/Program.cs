using MelonLoader;
using UnityEngine;

namespace YuchiGames.POM.Server
{
    public class Program : MelonMod
    {
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            string[] excludeObjects = new string[] { };
            foreach (GameObject obj in allObjects)
            {
                if (!excludeObjects.Contains(obj.name))
                    GameObject.Destroy(obj);
            }
        }
    }
}
