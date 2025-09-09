using System;

namespace YuchiGames.POM.Protocol
{
    public static class Wire
    {
        public const int SizeOfFloat = 4;
        public const int SizeOfVector3 = 3 * SizeOfFloat;
        public const int SizeOfQuaternion = 4 * SizeOfFloat;
        public const int SizeOfTransform = SizeOfVector3 + SizeOfQuaternion + SizeOfVector3;
        public const int SizeOfInt32 = 4;
        public const int CubeBaseSize = SizeOfTransform + 1 + SizeOfInt32;
    }
}
