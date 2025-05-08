using System;
using System.Collections;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Server;
using UnityEngine;
using YuchiGames.POM.Hooks;
using YuchiGames.POM.Network.Mqtt;
using YuchiGames.POM.Test;


namespace YuchiGames.POM.Test
{
    public static class RandomCubeBinaryWriter
    {
        /// <summary>
        /// 指定ファイルに、中心 center を軸に radius 範囲で
        /// count 個のランダム Transform(pos,rot,scale)＋Substance を
        /// 44 バイト単位で書き出す。
        /// </summary>
        public static void WriteRandomTransforms(
            string filePath,
            int count,
            Vector3 center,
            float radius = 5f,
            float minScale = 0.3f,
            float maxScale = 1.0f)
        {
            System.Random rand = new System.Random();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

            Substance[] allSubs = Enum.GetValues(typeof(Substance)) as Substance[];

            using (FileStream fs = File.Create(filePath))
            {
                for (int i = 0; i < count; i++)
                {
                    float dx = (float)(rand.NextDouble() * 2 * radius - radius);
                    float dz = (float)(rand.NextDouble() * 2 * radius - radius);
                    Vector3 pos = new Vector3(center.x + dx, center.y, center.z + dz);

                    Quaternion rot = Quaternion.Euler(
                        (float)(rand.NextDouble() * 360),
                        (float)(rand.NextDouble() * 360),
                        (float)(rand.NextDouble() * 360)
                    );

                    Vector3 scale = new Vector3(
                        (float)(rand.NextDouble() * (maxScale - minScale) + minScale),
                        (float)(rand.NextDouble() * (maxScale - minScale) + minScale),
                        (float)(rand.NextDouble() * (maxScale - minScale) + minScale)
                    );

                    Substance sub = allSubs[rand.Next(allSubs.Length)];

                    byte[] posBytes = TransformSerializer.Vector3ToBytes(pos);
                    byte[] rotBytes = TransformSerializer.QuaternionToBytes(rot);
                    byte[] scaleBytes = TransformSerializer.Vector3ToBytes(scale);
                    byte[] subBytes = BitConverter.GetBytes((int)sub);

                    fs.Write(posBytes, 0, posBytes.Length);
                    fs.Write(rotBytes, 0, rotBytes.Length);
                    fs.Write(scaleBytes, 0, scaleBytes.Length);
                    fs.Write(subBytes, 0, subBytes.Length);
                }
            }

            MelonLoader.MelonLogger.Msg($"Wrote {count} transforms (with Substance) to {filePath}");
        }
    }
}
