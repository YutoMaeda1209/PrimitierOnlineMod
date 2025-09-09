namespace YuchiGames.POM.Protocol
{
    public static class Topic
    {
        public const string WorldSeed = "world/seed";

        public static string PlayerTransform(int playerId) => $"player/{playerId}/transform"; // TODO ぜっっっっっっっっったい検証コード作ったほうがいい
        public const  string PlayerTransformFilter = "player/+/transform";

        public static string CubeBase(string cubeId) => $"world/cubeBase/{cubeId}"; // TODO ぜっっっったい検証コード作れ作れ作れ作れ作れ
        public const  string CubeBaseFilter = "world/cubeBase/+";
    }
}
