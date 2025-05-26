using System;
using System.Diagnostics;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using YuchiGames.POM;
using YuchiGames.POM.Network.Mqtt;
using YuchiGames.POM.Hooks;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace YuchiGames.POM.Hooks
{
    [HarmonyPatch(typeof(CubeGenerator), nameof(CubeGenerator.GenerateCube),
        new Type[] {
            typeof(Vector3),
            typeof(Vector3),
            typeof(Substance),
            typeof(CubeAppearance.SectionState),
            typeof(CubeAppearance.UVOffset),
            typeof(string)
        })]
    [HarmonyPatch(typeof(CubeGenerator), nameof(CubeGenerator.GenerateCube),
        new Type[] {
            typeof(Vector3),
            typeof(Quaternion),
            typeof(Vector3),
            typeof(Substance),
            typeof(CubeAppearance.SectionState),
            typeof(CubeAppearance.UVOffset),
            typeof(string)
        })]
    public static class GenerateCubePatch
    {
        static bool Prefix()
        {
            var caller = new StackFrame(2, true).GetMethod();
            MelonLogger.Msg($"{caller}");
            return Program.isHost;
        }

        static void Postfix(CubeBase __result)
        {
            try
            {
                // CubeID の生成
                byte[] cubeID = CubeBaseIDGenerator.GenerateID(__result);
                var idHolder = __result.gameObject.AddComponent<CubeIDHolder>();
                idHolder.CubeID = cubeID;

                // Transform 情報をバイト列に変換
                byte[] posBytes = TransformSerializer.Vector3ToBytes(__result.transform.position);
                byte[] rotBytes = TransformSerializer.QuaternionToBytes(__result.transform.rotation);
                byte[] scaleBytes = TransformSerializer.Vector3ToBytes(__result.transform.localScale);
                byte[] subBytes = BitConverter.GetBytes((int)__result.substance);

                // 44 バイトのペイロードを組み立て
                byte[] payload = new byte[44];
                Buffer.BlockCopy(posBytes, 0, payload, 0, 12);
                Buffer.BlockCopy(rotBytes, 0, payload, 12, 16);
                Buffer.BlockCopy(scaleBytes, 0, payload, 28, 12);
                Buffer.BlockCopy(subBytes, 0, payload, 40, 4);

                // トピックに CubeID を含めて Publish
                string topic = $"world/{BitConverter.ToString(cubeID)}/cubeBase";
                _ = Program.Instance.Mqtt.PublishAsync(
                    topic,
                    payload,
                    qos: 2,
                    retain: true
                );

                MelonLogger.Msg($"[MQTT] Published CubeBase ID={BitConverter.ToString(cubeID)}");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error in CubeBase Postfix: {e}");
            }
            return;
        }
    }

    [HarmonyPatch(typeof(CubeGenerator), nameof(CubeGenerator.GenerateNewChunk))]
    public static class GenerateChunkPatch
    {
        static bool Prefix()
        {
            var caller = new StackFrame(1, true).GetMethod();
            MelonLogger.Msg($"{caller}");
            return Program.isHost;
        }
    }
}
