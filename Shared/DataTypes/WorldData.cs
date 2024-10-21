using Il2Cpp;
using MessagePack;

namespace YuchiGames.POM.Shared.DataTypes
{
    [MessagePackObject]
    public class SWorldData
    {
        [Key(0)]
        public bool IsCreativeMode { get; set; }
        [Key(1)]
        public int Seed { get; set; }
        [Key(2)]
        public float TerrainHorizontalScale { get; set; }
        [Key(3)]
        public float TerrainVerticalScale { get; set; }
        [Key(4)]
        public float Time { get; set; }
        [Key(5)]
        public List<SUserData> Users { get; set; } = new List<SUserData>();
        [Key(6)]
        public List<SChunkData> Chunks { get; set; } = new List<SChunkData>();
    }

    [MessagePackObject]
    public struct SUserData
    {
        [Key(0)]
        public string UserID { get; set; }
        [Key(1)]
        public SVector3 CurrentPosition { get; set; }
        [Key(2)]
        public float CurrentAngle { get; set; }
        [Key(3)]
        public float MaxLife { get; set; }
        [Key(4)]
        public float Life { get; set; }
        [Key(5)]
        public SVector3 RespawnPosition { get; set; }
        [Key(6)]
        public float RespawnAngle { get; set; }
        [Key(7)]
        public SVector3 CameraPosition { get; set; }
        [Key(8)]
        public SQuaternion CameraRotation { get; set; }
        [Key(9)]
        public SVector3[] HolsterPositions { get; set; }
    }

    [MessagePackObject]
    public struct SChunkData
    {
        [Key(0)]
        public SVector2Int Position { get; set; }
        [Key(1)]
        public List<SGroupData> GroupData { get; set; }
    }

    [MessagePackObject]
    public struct SGroupData
    {
        [Key(0)]
        public SVector3 Position { get; set; }
        [Key(1)]
        public SQuaternion Rotation { get; set; }
        [Key(2)]
        public List<SCubeData> CubeData { get; set; }
    }

    [MessagePackObject]
    public struct SCubeData
    {
        [Key(0)]
        public SVector3 Position { get; set; }
        [Key(1)]
        public SQuaternion Rotation { get; set; }
        [Key(2)]
        public SVector3 Scale { get; set; }
        [Key(3)]
        public CubeConnector.Anchor Anchor { get; set; }
        [Key(4)]
        public List<string> Behaviors { get; set; }
        [Key(5)]
        public float BurnedRatio { get; set; }
        [Key(6)]
        public List<int> Connections { get; set; }
        [Key(7)]
        public bool IsBurning { get; set; }
        [Key(8)]
        public float LifeRatio { get; set; }
        [Key(9)]
        public CubeName CubeName { get; set; }
        [Key(10)]
        public CubeAppearance.SectionState SectionState { get; set; }
        [Key(11)]
        public List<string> States { get; set; }
        [Key(12)]
        public Substance Substance { get; set; }
        [Key(13)]
        public float Temperature { get; set; }
        [Key(14)]
        public CubeAppearance.UVOffset UVOffset { get; set; }
    }
}