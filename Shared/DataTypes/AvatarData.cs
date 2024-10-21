using MessagePack;

namespace YuchiGames.POM.Shared.DataTypes
{
    [MessagePackObject]
    public class SPlayerPositionData
    {
        [Key(0)]
        public STransform HeadTransform { get; set; }
        [Key(1)]
        public STransform LeftHandTransform { get; set; }
        [Key(2)]
        public STransform RightHandTransform { get; set; }
    }
}