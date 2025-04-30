using System.Text;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using YuchiGames.POM;

namespace YuchiGames.POM.Hooks
{
    [HarmonyPatch(typeof(CubeBase), nameof(CubeBase.Initialize))]
    public static class CubeBaseInitializePatch
    {
        static void Postfix(CubeBase __instance)
        {
            if (__instance.gameObject.GetComponent<CubeIDHolder>() != null)
                return;
            Vector3 pos = __instance.transform.position;
            Vector2Int chunk = CubeGenerator.WorldToChunkPos(pos);

            byte[] cubeID = CubeBaseIDGenerator.GenerateID(__instance);

            CubeIDHolder idHolder = __instance.gameObject.AddComponent<CubeIDHolder>();
            idHolder.CubeID = cubeID;
        }
    }
}
