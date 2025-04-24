using MelonLoader;
using UnityEngine;

namespace YuchiGames.POM
{
    [RegisterTypeInIl2Cpp]
    public class CubeIDHolder : MonoBehaviour
    {
        public string CubeID;

        public CubeIDHolder(IntPtr ptr) : base(ptr) { }
    }
}
