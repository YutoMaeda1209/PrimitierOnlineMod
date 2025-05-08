using System.Text;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using YuchiGames.POM;
using YuchiGames.POM.Network.Mqtt;
using YuchiGames.POM.Hooks;
using System.Threading.Tasks;
using System;

namespace YuchiGames.POM.Hooks
{
    [HarmonyPatch(typeof(CubeBase), nameof(CubeBase.Initialize))]
    public static class CubeBaseInitializePatch
    {
        // デフォルト生成のフラグだけ
        private static bool isDefaultGeneration = true;

        static bool Prefix()
        {
            // Brokerから取得してない場合はスキップ
            return isDefaultGeneration;
        }

        static void Postfix(CubeBase __instance)
        {
            if (!isDefaultGeneration) return;

            if (__instance.gameObject.GetComponent<CubeIDHolder>() != null)
                return;

            try
            {
                byte[] cubeID = CubeBaseIDGenerator.GenerateID(__instance);
                CubeIDHolder idHolder = __instance.gameObject.AddComponent<CubeIDHolder>();
                idHolder.CubeID = cubeID;

                // トランスフォーム情報の取得
                byte[] posBytes = TransformSerializer.Vector3ToBytes(__instance.transform.position);
                byte[] rotBytes = TransformSerializer.QuaternionToBytes(__instance.transform.rotation);
                byte[] scaleBytes = TransformSerializer.Vector3ToBytes(__instance.transform.localScale);
                byte[] subBytes = BitConverter.GetBytes((int)__instance.substance);

                // ペイロードの構築
                int totalLen = 44; // CubeIDは topic に含めるため、ペイロードには含めないやで
                byte[] payload = new byte[totalLen];
                Buffer.BlockCopy(posBytes, 0, payload, 0, 12);
                Buffer.BlockCopy(rotBytes, 0, payload, 12, 16);
                Buffer.BlockCopy(scaleBytes, 0, payload, 28, 12);
                Buffer.BlockCopy(subBytes, 0, payload, 40, 4);

                // CubeIDを含むトピックの作成
                string topic = string.Format("world/{0}/cubeBase",BitConverter.ToString(cubeID));

                // FIXME デバッグのためにQoS2にしてるます
                _ = Program.Instance.Mqtt.PublishAsync(
                    topic,
                    payload,
                    qos: 2,
                    retain: false
                );

                // FIXME Substanceの設定ミスってるかも。取得側の問題か調査中
                var field = __instance.GetType().GetField("substance",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

                MelonLogger.Msg(
                    $"[MQTT] Published CubeBase ID={BitConverter.ToString(cubeID)}"
                );
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error in CubeBase Postfix: {e.Message}");
            }
        }

        // ゲーム由来のCubeBase生成を切る
        public static void DisableDefaultGeneration()
        {
            isDefaultGeneration = false;
            MelonLogger.Msg($"デフォルト生成をOFFにしたよ");
        }
    }
}
