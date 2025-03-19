using Il2Cpp;
using UnityEngine;
using System.Collections;
using MelonLoader;
using YuchiGames.POM.Network.Mqtt;

namespace YuchiGames.POM.Hooks
{
    public class WorldLauncher
    {
        public static WorldLauncher Instance { get; set; } = new WorldLauncher();

        /// <summary>
        /// MQTTで受信したseedメッセージをメインスレッドで処理し、ワールド開始処理を実行します。
        /// </summary>
        /// <param name="topic">受信したトピック</param>
        /// <param name="payload">受信したpayload（seedの文字列）</param>
        /// <returns>IEnumerator（コルーチン用）</returns>
        public IEnumerator ProcessSeedMessageCoroutine(string topic, string payload)
        {
            // 次フレームに待機（これによりメインスレッド上で実行されることが保証される）
            yield return null;

            if (!Int32.TryParse(payload, out int seed))
            {
                MelonLogger.Error("受信したseedが不正です: " + payload);
                yield break;
            }

            // 必要なGameObject（NewGameSettings）の取得
            NewGameSettings newGameSettings = GameObject.FindObjectOfType<NewGameSettings>(true);
            if (newGameSettings == null)
            {
                MelonLogger.Error("NewGameSettingsが見つかりません。");
                yield break;
            }

            // 受信したseedを設定し、ゲーム開始処理を実行
            newGameSettings.seedInputField.text = seed.ToString();
            newGameSettings.terrainVerticalScale = 1.0f;
            newGameSettings.terrainHorizontalScale = 1.0f;

            MelonLogger.Msg("ワールド開始処理を実行します。Seed: " + seed);
            newGameSettings.StartNewGame();
        }
    }
}
