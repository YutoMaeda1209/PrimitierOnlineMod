using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using Il2Cpp;
using MelonLoader;
using Unity.XR.CoreUtils;
using UnityEngine;
using System.Text;

namespace YuchiGames.POM
{

    public static class CubeBaseIDGenerator
    {
        private static Dictionary<string, int> counters = new Dictionary<string, int>();
        public static string GenerateID(CubeBase cube)
        {
            Vector3 pos = cube.transform.position;
            Vector2Int chunk = CubeGenerator.WorldToChunkPos(pos);
            string id = chunk.x + ":" + chunk.y;
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(id)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(""+pos.x+pos.y+pos.z));
                id = BitConverter.ToString(hashBytes).Replace("-", String.Empty);
            }
            return id;
        }
    }
}
