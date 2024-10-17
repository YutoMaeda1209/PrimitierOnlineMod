using HarmonyLib;
using Il2Cpp;
using YuchiGames.POM.Client.Assets;

namespace YuchiGames.POM.Client.Patches
{
    [HarmonyPatch(typeof(HeightCalibrator), nameof(HeightCalibrator.ShowTitleMenu))]
    class HeightCalibrator_ShowTitleMenuPatch
    {
        private static void Postfix()
        {

        }
    }
}
