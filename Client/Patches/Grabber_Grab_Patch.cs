
using HarmonyLib;
using Il2Cpp;
using YuchiGames.POM.Client.Managers;

namespace YuchiGames.POM.Client.Patches
{
    [HarmonyPatch(typeof(Grabber), nameof(Grabber.Grab))]
    public static class Grabber_Grab_Patch
    {
        // __0 - is remote
        public static void Postfix(Grabber __instance, bool __0)
        {
            if (__instance.bond != null)
                Network.ClaimHost(__instance.bond.GetComponent<GroupSyncerComponent>().GroupUID);
        }
    }
}
