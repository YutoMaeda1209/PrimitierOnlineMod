
using HarmonyLib;
using MelonLoader;

namespace YuchiGames.POM.Client.Patches
{
    [HarmonyPatch("Il2CppInterop.HarmonySupport.Il2CppDetourMethodPatcher", "ReportException")]
    public static class Il2CppDetourMethodPatcher_Patch
    {
        public static bool Prefix(System.Exception ex)
        {
            MelonLogger.Error("During invoking native->managed trampoline", ex);
            return false;
        }
    }
}
