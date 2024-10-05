using Il2Cpp;
using YuchiGames.POM.Shared.DataObjects;
using UnityEngine;
using YuchiGames.POM.Shared;
using YuchiGames.POM.Shared.Utils;

namespace YuchiGames.POM.Client.Managers
{
    public static class World
    {
        public static LocalWorldData WorldData { get; private set; } = new();
        public static SaveAndLoad.SaveData SaveData { get; private set; } = new();

        public static void LoadWorldData(LocalWorldData localWorldData)
        {
            WorldData = localWorldData;
            SaveData = new()
            {
                seed = WorldData.Seed,
                time = WorldData.Time,
                playerMaxLife = WorldData.PlayerMaxLife,
                playerPos = WorldData.Player.Position.ToUnity(),
                playerAngle = 0f,
                playerLife = WorldData.Player.Life,
                respawnPos = WorldData.RespawnPosition.ToUnity(),
                respawnAngle = 0f,
                holsterPositions = new Il2CppSystem.Collections.Generic.List<Vector3>().Apply(l =>
                {
                    l.Add(WorldData.Player.LeftHolsterPosition.ToUnity());
                    l.Add(WorldData.Player.RightHolsterPosition.ToUnity());
                }),
                chunks = new(),
            };
        }
    }
}