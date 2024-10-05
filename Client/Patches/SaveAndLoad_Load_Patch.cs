using Il2Cpp;
using HarmonyLib;
using YuchiGames.POM.Client.Managers;

namespace YuchiGames.POM.Client.Patches
{
    [HarmonyPatch(typeof(SaveAndLoad), nameof(SaveAndLoad.Load))]
    class SaveAndLoad_Load_Patch
    {
        private static void Prefix(ref SaveAndLoad.SaveData saveData)
        {
            saveData = World.SaveData;
        }
    }
}