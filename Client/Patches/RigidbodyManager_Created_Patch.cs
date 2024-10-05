
using HarmonyLib;
using Il2Cpp;
using YuchiGames.POM.Client.Managers;

namespace YuchiGames.POM.Client.Patches
{
    [HarmonyPatch(typeof(RigidbodyManager), nameof(RigidbodyManager.Start))]
    public static class RigidbodyManager_Created_Patch
    {
        public static void Postfix(RigidbodyManager __instance)
        {
            if (__instance.gameObject.GetComponent<GroupSyncerComponent>() == null)
                __instance.gameObject.AddComponent<GroupSyncerComponent>();
        }
    }
}
