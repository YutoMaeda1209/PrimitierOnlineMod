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
using Il2CppInterop.Runtime;
using System.Collections;

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

                // 診断用モニタを取り付け（フレームとアンカー変化を追跡）
                try
                {
                    var diag = __result.gameObject.AddComponent<FramePhaseDiagnostics>();
                    diag.TargetCube = __result;
                    diag.MaxFramesToTrack = 300; // 約5秒@60fps
                    diag.StopWhenAnchorResolved = false; // 最初は全フェーズ観測
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Attach diagnostics failed: {ex}");
                }

                // ==== Hierarchy Debug (CubeBase -> parent -> grandparent) ====
                try
                {
                    Transform cur = __result.transform;
                    for (int level = 0; level < 3 && cur != null; level++)
                    {
                        var go = cur.gameObject;
                        var comps = go.GetComponents(Il2CppType.Of<Component>());
                        MelonLogger.Error($"[Hierarchy L{level}] GO='{go.name}' activeSelf={go.activeSelf} children={cur.childCount}");
                        if (comps != null)
                        {
                            for (int i = 0; i < comps.Length; i++)
                            {
                                var c = comps[i];
                                if (c != null)
                                {
                                    MelonLogger.Error($"  - Comp[{i}] {c.GetType().FullName}");
                                }
                            }
                        }
                        cur = cur.parent;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Hierarchy debug failed: {ex}");
                }

                // Anchor を取得（親オブジェクト側から探索。なければ Free 扱い）
				var connector = __result.GetComponentInParent<CubeConnector>();
                MelonLogger.Error("walnut debug : " + __result);
                var compornents = __result.GetComponentsInParent<CubeConnector>();
                foreach (var con in compornents)
                {
                    MelonLogger.Error(con);
                }
				if (connector == null)
				{
					var parent = __result.transform.parent;
					if (parent != null)
					{
						connector = parent.GetComponentInChildren<CubeConnector>(true);
					}
					MelonLogger.Msg("walnut-connector : " + connector);
				}
				CubeConnector.Anchor anchor = CubeConnector.Anchor.Free;
				if (connector != null)
				{
					anchor = connector.anchor;
					MelonLogger.Msg("walnut-anchor : " + anchor);
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

                // 生成直後は Connector が未アタッチの可能性があるため、数フレーム後に再チェックして更新を試みる
                MelonCoroutines.Start(GenerateCubePatch_Utils.CaptureAnchorAndRepublish(__result, cubeID));
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error in CubeBase Postfix: {e}");
            }
        }

    }

    // 生成後に Anchor を数フレームリトライで取得し、見つかれば再送信する
    public static class GenerateCubePatch_Utils
    {
        public static IEnumerator CaptureAnchorAndRepublish(CubeBase cubeBase, byte[] cubeID)
        {
            // 最大10フレームまで待ちつつ探索
            for (int i = 0; i < 10; i++)
            {
                if (cubeBase == null) yield break;

                // 親方向に Type ベースで探索（IL2CPP安全）
                CubeConnector connector = null;
                Transform parent = cubeBase.transform;
                for (int depth = 0; depth < 3 && parent != null && connector == null; depth++)
                {
                    var comp = parent.GetComponent(Il2CppType.Of<CubeConnector>());
                    connector = comp as CubeConnector;
                    if (connector == null)
                    {
                        var comps = parent.GetComponentsInChildren(Il2CppType.Of<CubeConnector>(), true);
                        if (comps != null && comps.Length > 0)
                        {
                            connector = comps[0] as CubeConnector;
                        }
                    }
                    if (connector == null) parent = parent.parent;
                }

                if (connector != null)
                {
                    var anchor = connector.anchor;
                    // Free 以外になったら再送信
                    if (anchor != CubeConnector.Anchor.Free)
                    {
                        try
                        {
                            Transform t = cubeBase.transform;
                            byte[] posBytes   = TransformCodec.Vector3ToBytes(t.position);
                            byte[] rotBytes   = TransformCodec.QuaternionToBytes(t.rotation);
                            byte[] scaleBytes = TransformCodec.Vector3ToBytes(t.localScale);
                            byte[] subBytes   = BitConverter.GetBytes((int)cubeBase.substance);

                            const int PayloadSize = 45;
                            byte[] payload = new byte[PayloadSize];
                            Buffer.BlockCopy(posBytes,   0, payload,  0, 12);
                            Buffer.BlockCopy(rotBytes,   0, payload, 12, 16);
                            Buffer.BlockCopy(scaleBytes, 0, payload, 28, 12);
                            byte flags = 0;
                            int a = (int)anchor; if ((uint)a > 2u) a = 0;
                            payload[40] = (byte)((flags & 0b1111_1100) | (a & 0b11));
                            Buffer.BlockCopy(subBytes, 0, payload, 41, 4);

                            string cubeIdHex = BitConverter.ToString(cubeID).Replace("-", "");
                            string topic = Topic.CubeBase(cubeIdHex);
                            _ = Program.Instance.Mqtt.PublishAsync(topic, payload, qos: 2, retain: true);
                            MelonLogger.Msg($"[MQTT] Republished CubeBase (anchor updated) Anchor={anchor} → {topic}");
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Warning($"Republish after anchor capture failed: {ex}");
                        }
                        yield break;
                    }
                }

                // 次フレームまで待機
                yield return null;
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
