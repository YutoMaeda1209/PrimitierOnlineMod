using MelonLoader;
using UnityEngine;

namespace YuchiGames.POM.Hooks
{
    [RegisterTypeInIl2Cpp]
    public class CubeIDHolder : MonoBehaviour
    {
        public byte[] CubeID;

        public CubeIDHolder(IntPtr ptr) : base(ptr) { }
    }
}
