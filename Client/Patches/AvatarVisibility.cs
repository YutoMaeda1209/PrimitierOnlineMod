﻿using HarmonyLib;
using Il2Cpp;

namespace YuchiGames.POM.Client.Patches
{
    [HarmonyPatch(typeof(AvatarVisibility), nameof(AvatarVisibility.ChangeAvatarVisibility))]
    class AvatarVisibility_ChangeAvatarVisibility
    {
        private static void Prefix(ref bool isVisible)
        {
            isVisible = false;
        }
    }
}