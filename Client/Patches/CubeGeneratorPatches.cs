
using Client;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using YuchiGames.POM.Client.Managers;
using YuchiGames.POM.Shared;
using YuchiGames.POM.Shared.Utils;

namespace YuchiGames.POM.Client.Patches
{
    [HarmonyPatch(typeof(CubeGenerator), nameof(CubeGenerator.GenerateChunk))]
    public static class CubeGenerator_GenerateChunk_Patch
    {
        public static bool Prefix(Vector2Int __0)
        {
            if (!Network.IsConnected)
                return true;

            Network.Send(new RequestNewChunkDataMessage() { ChunkPos = __0.ToShared() });
            return false;
        }
    }
    [HarmonyPatch(typeof(CubeGenerator), nameof(CubeGenerator.DestroyChunks))]
    public static class CubeGenerator_DestroyChunk_Patch
    {
        public static void Prefix(Il2CppSystem.Collections.Generic.List<Vector2Int> __0)
        {
            foreach (var obj in Network.SyncedObjects.Values)
                if (__0.Contains(CubeGenerator.WorldToChunkPos(obj.transform.position)))
                    obj.Unload();
        } 
        public static void Postfix(Il2CppSystem.Collections.Generic.List<Vector2Int> __0)
        {
            if (Network.IsConnected)
            {
                foreach (var pos in __0)
                {
                    Network.Send(new ChunkUnloadMessage()
                    {
                        Pos = pos.ToShared(),
                    });

                    Network.Send(new SavedChunkDataMessage()
                    {
                        Chunk = DataConverter.ToChunk(SaveAndLoad.chunkDict[pos]),
                        Pos = pos.ToShared()
                    });
                }
            }
        }
    }

    [HarmonyPatch(typeof(RigidbodyManager), nameof(RigidbodyManager.OnCollisionEnter))]
    public static class RigidbodyManager_OnCollisionEnter_Patch
    {
        public static bool Prefix(RigidbodyManager __instance, Collision __0)
        {
            return __instance.GetComponent<GroupSyncerComponent>()?.Apply(syncer => syncer.OnCollisionStay(__0)).IsHosted ?? true;
        }
    }
    // FIXME: Only if health changed
    [HarmonyPatch(typeof(CubeBase), nameof(CubeBase.life), MethodType.Setter)]
    public static class CubeBase_OnCollisionEnterReceived_Patch
    {
        public static void Prefix(CubeBase __instance, float __0)
        {
            if (__instance.life >= __0)
                return;

            __instance.transform.parent.gameObject.GetComponent<GroupSyncerComponent>()?.Apply(syncer =>
            {
                if (!syncer.IsHosted)
                    return;

                Network.Send(new CubeDamageTakedMessage()
                {
                    GroupUID = syncer.GroupUID,
                    CubeIndex = __instance.transform.ChildIndex(),
                    Health = __instance.life
                });
            });
        }
    }
}
