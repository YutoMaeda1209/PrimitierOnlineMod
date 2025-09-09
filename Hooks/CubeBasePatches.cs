using System;
using System.Diagnostics;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using YuchiGames.POM;
using YuchiGames.POM.Network.Mqtt;
using YuchiGames.POM.Hooks;
using YuchiGames.POM.Protocol;
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

            // 内部生成の場合はスキップ（無限ループ防止）
            lock (Program._lockObject)
            {
                if (Program._isInternalGeneration)
                {
                    MelonLogger.Msg("Skipping CubeBase generation (internal call)");
                    return true; // 元のメソッドを実行
                }
            }

            return Program.isHost;
        }

        static void Postfix(CubeBase __result)
        {
            try
            {
                // 内部生成の場合はスキップ（無限ループ防止）
                lock (Program._lockObject)
                {
                    if (Program._isInternalGeneration)
                    {
                        return;
                    }
                }

                // null チェックを追加
                if (__result == null)
                {
                    // GenerateNewChunkからの呼び出しの場合は正常な動作なので警告レベルに下げる
                    var caller = new StackFrame(2, true).GetMethod();
                    if (caller?.Name?.Contains("GenerateNewChunk") == true)
                    {
                        MelonLogger.Msg("CubeBase Postfix: __result is null (from GenerateNewChunk - this is normal)");
                    }
                    else
                    {
                        MelonLogger.Warning("CubeBase Postfix: __result is null (unexpected)");
                    }
                    return;
                }

                // CubeID 生成と保持
                byte[] cubeID = CubeBaseIDGenerator.GenerateID(__result);
                var idHolder = __result.gameObject.AddComponent<CubeIDHolder>();
                idHolder.CubeID = cubeID;

                // Anchor を取得（なければ Free 扱い）
                CubeConnector connector = __result.GetComponentInChildren<CubeConnector>(true);
                var anchor = CubeConnector.Anchor.Free;  // デフォルト値を設定
                if (connector != null)
                {
                    MelonLogger.Msg("walnut : " + connector.anchor);
                    anchor = connector.anchor;
                }

                // Transform を 40B にエンコード
                Transform t = __result.transform;
                byte[] posBytes   = TransformCodec.Vector3ToBytes(t.position);    // 12
                byte[] rotBytes   = TransformCodec.QuaternionToBytes(t.rotation); // 16
                byte[] scaleBytes = TransformCodec.Vector3ToBytes(t.localScale);  // 12

                // Substance
                byte[] subBytes = BitConverter.GetBytes((int)__result.substance);      // 4

                // ---- ここが肝：45バイト固定ワイヤ ----
                // [0..39] Transform, [40] Header( flags<<2 | anchor2bit ), [41..44] Substance(int32)
                const int PayloadSize = 45;
                byte[] payload = new byte[PayloadSize];

                Buffer.BlockCopy(posBytes,   0, payload,  0, 12);
                Buffer.BlockCopy(rotBytes,   0, payload, 12, 16);
                Buffer.BlockCopy(scaleBytes, 0, payload, 28, 12);

                byte flags = 0;
                int a = (int)anchor;
                if ((uint)a > 2u) a = 0;                         // 2bit 範囲に正規化
                payload[40] = (byte)((flags & 0b1111_1100) | (a & 0b11));  // インデックスを45から40に修正

                Buffer.BlockCopy(subBytes, 0, payload, 41, 4);   // ★ ここが以前と違う（40ではなく41）

                // トピック（IDはハイフン無しのHEX推奨）
                string cubeIdHex = BitConverter.ToString(cubeID).Replace("-", "");
                string topic = Topic.CubeBase(cubeIdHex);

                _ = Program.Instance.Mqtt.PublishAsync(topic, payload, qos: 2, retain: true);

                MelonLogger.Msg($"[MQTT] Published CubeBase len={payload.Length} Anchor={anchor} Substance={__result.substance} → {topic}");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error in CubeBase Postfix: {e}");
            }
        }

    }

    [HarmonyPatch(typeof(CubeGenerator), nameof(CubeGenerator.GenerateNewChunk))]
    public static class GenerateChunkPatch
    {
        static bool Prefix()
        {
            var caller = new StackFrame(1, true).GetMethod();
            MelonLogger.Msg($"GenerateNewChunk called by: {caller}");

            // チャンク生成は常に許可（地形生成のため）
            return true;
        }
    }
}
