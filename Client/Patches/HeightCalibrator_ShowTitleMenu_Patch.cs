using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace YuchiGames.POM.Client.Patches
{
    [HarmonyPatch(typeof(HeightCalibrator), nameof(HeightCalibrator.ShowTitleMenu))]
    class HeightCalibrator_ShowTitleMenu_Patch
    {
        private static void Postfix()
        {
            Assets.StartButton.Initialize();

            GameObject titleMainCanvas = GameObject.Find(
                "/TitleSpace/TitleMenu/MainCanvas");
            titleMainCanvas.transform.Find("AvatarVisibilityButton").gameObject.SetActive(false);
            titleMainCanvas.transform.Find("AvatarScale").gameObject.SetActive(false);

            Transform mainCanvasObject = GameObject.Find(
                "/Player/XR Origin/Camera Offset/LeftHand Controller/RealLeftHand/MenuWindowL/Windows/MainCanvas")
                .transform;

            Transform systemTabObject = mainCanvasObject.Find("SystemTab");
            systemTabObject.Find("BlueprintButton").localPosition = new Vector3(-120, 180, 0);
            systemTabObject.Find("ResetHolsterButton").localPosition = new Vector3(-40, 180, 0);
            systemTabObject.Find("RecalibrateButton").localPosition = new Vector3(40, 180, 0);
            systemTabObject.Find("DieButton").localPosition = new Vector3(120, 180, 0);
            systemTabObject.Find("ExitButton").localPosition = new Vector3(-120, 100, 0);

            Transform settingsTabObject = mainCanvasObject.Find("SettingsTab");
            settingsTabObject.Find("DayNightCycleButton").localScale = new Vector3(0, 0, 0);
            settingsTabObject.Find("SnapSettingButton").localPosition = new Vector3(-120, 190, 0);
            settingsTabObject.Find("TurnSettingButton").localPosition = new Vector3(-40, 190, 0);

            Transform distanceSettingsObject = settingsTabObject.Find("DistanceSettings");
            distanceSettingsObject.gameObject.SetActive(false);
        }
    }
}