using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Il2Cpp;
using MelonLoader;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace YuchiGames.POM.Hooks
{

    public static class CubeBaseIDGenerator
    {
        private static Dictionary<string, int> counters = new Dictionary<string, int>();
        public static byte[] GenerateID(CubeBase cube)
        {
            Vector3 pos = cube.transform.position;
            Vector2Int chunk = CubeGenerator.WorldToChunkPos(pos);
            string id = $"{chunk.x}{chunk.y}";
            byte[] hashBytes = new byte[32];
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(id)))
            {
                hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{pos.x}{pos.y}{pos.z}"));
            }
            StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("X2"));
            MelonLogger.Msg(sb.ToString());
            return hashBytes;
        }
    }
}
