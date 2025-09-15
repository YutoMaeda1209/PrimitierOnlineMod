using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Il2Cpp;
using Il2CppSystem.Xml.Schema;
using MelonLoader;
using UnityEngine;
using YuchiGames.POM.Hooks;
using YuchiGames.POM.Network.Mqtt;

namespace YuchiGames.POM.Instance
{
    struct PositionPacket
    {
        public ushort SequenceId;
        public long Timestamp;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    /// <summary>
    /// 他のプレイヤーのアバター管理と、自身のアバター位置情報の送信を行います。
    /// </summary>
    public class AvatarManager
    {
        /// <summary>
        /// <see cref="AvatarManager"/> のシングルトンインスタンスです。
        /// </summary>
        public static AvatarManager? Instance { get; set; }

        private Transform _headTransform = new Transform();
        private Transform _leftHandTransform = new Transform();
        private Transform _rightHandTransform = new Transform();
        private InstanceManager _instanceManager;
        private List<string> _playerIds = new List<string>();
        private Queue<PositionPacket> _posPacketQueue = new Queue<PositionPacket>();

        public AvatarManager()
        {
            MqttManager mqttManager = MqttManager.Instance
                ?? throw new NullReferenceException("MqttManager instance is null in AvatarManager constructor!");
            _ = mqttManager.RegisterCallbackAndSubscribeAsync("player/+/status", 2, HandleAvatarStatus);
            _ = mqttManager.RegisterCallbackAndSubscribeAsync("player/+/transform/+", 0, HandleAvatarTransform);

            AvatarVisibility avatarVisibility = GameObject.FindObjectOfType<AvatarVisibility>()
                ?? throw new NullReferenceException("AvatarVisibility not found in the scene.");
            _headTransform = avatarVisibility.proxyHead.transform;
            _leftHandTransform = avatarVisibility.proxyLeftHand.transform;
            _rightHandTransform = avatarVisibility.proxyRightHand.transform;

            _instanceManager = InstanceManager.Instance
                ?? throw new NullReferenceException("InstanceManager instance is null in AvatarManager constructor!");
            string playerId = _instanceManager.PlayerId;
            MelonCoroutines.Start(SendAvatarPosCoroutine(playerId));
        }

        private void HandleAvatarStatus(string topic, byte[] payload)
        {
            string playerId = ParsePlayerIdFromTopic(topic);
            if (playerId == string.Empty)
            {
                Melon<Program>.Logger.Warning($"Failed to extract player name from topic: {topic}");
                return;
            }

            if (playerId == _instanceManager.PlayerId)
                return;

            string status;
            try
            {
                status = Encoding.UTF8.GetString(payload);
            }
            catch (DecoderFallbackException exception)
            {
                Melon<Program>.Logger.Warning($"Failed to decode payload for player '{playerId}': {exception.Message}");
                return;
            }
            switch (status)
            {
                case "joined":
                    try
                    {
                        _playerIds.Add(playerId);
                        Melon<Program>.Logger.Msg($"Current players: {string.Join(", ", _playerIds)}");
                        MelonCoroutines.Start(AvatarFactory.Create(playerId));
                        Melon<Program>.Logger.Msg($"Player {playerId} has joined the game.");
                    }
                    catch (ArgumentException exception)
                    {
                        Melon<Program>.Logger.Warning($"Failed to create avatar for player '{playerId}': {exception.Message}");
                        _playerIds.Remove(playerId);
                        MelonCoroutines.Start(AvatarFactory.Destroy(playerId));
                    }
                    break;
                case "left":
                    if (_playerIds.Contains(playerId))
                    {
                        _playerIds.Remove(playerId);
                        MelonCoroutines.Start(AvatarFactory.Destroy(playerId));
                        Melon<Program>.Logger.Msg($"Player {playerId} has left the game.");
                    }
                    break;
                default:
                    Melon<Program>.Logger.Warning($"Received unknown status '{status}' for player '{playerId}'");
                    break;
            }

            Melon<Program>.Logger.Msg($"Current players: {string.Join(", ", _playerIds)}");
        }

        private void HandleAvatarTransform(string topic, byte[] payload)
        {
            Melon<Program>.Logger.Msg("HandleAvatarTransform called.");

            string playerId = ParsePlayerIdFromTopic(topic);
            if (playerId == string.Empty)
            {
                Melon<Program>.Logger.Warning($"Failed to extract player name from topic: {topic}");
                return;
            }

            if (playerId == _instanceManager.PlayerId)
            {
                Melon<Program>.Logger.Warning("Ignoring transform update for own player.");
                return;
            }
            else if (!_playerIds.Contains(playerId))
            {
                Melon<Program>.Logger.Warning($"Received transform update for unknown player: {playerId}");
                return;
            }

            Hooks.Avatar? avatar = AvatarFactory.AllAvatars.GetValueOrDefault(playerId);
            if (avatar == null)
            {
                Melon<Program>.Logger.Warning($"Avatar not found for player: {playerId}");
                return;
            }

            Melon<Program>.Logger.Msg("Avatar found for player.");
            string pattern = @"^player/[^/]+/transform/([^/]+)$";
            Match match = Regex.Match(topic, pattern);
            if (!match.Success)
            {
                Melon<Program>.Logger.Warning($"Failed to extract transform part from topic: {topic}");
                return;
            }
            string partName = match.Groups[1].Value;

            PositionPacket packet = new PositionPacket();
            packet.SequenceId = 0;
            packet.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            packet.Position = Vector3.zero;
            packet.Rotation = Quaternion.identity;

            // Task.Run(() => AvatarSetTransformCoroutine(avatar, partName, payload));

            // MelonCoroutines.Start(AvatarSetTransformCoroutine(avatar, partName, payload));

            // Hooks.Avatar.AvatarPartType partType;
            // switch (partName)
            // {
            //     case "head":
            //         partType = Hooks.Avatar.AvatarPartType.Head;
            //         break;
            //     case "left-hand":
            //         partType = Hooks.Avatar.AvatarPartType.LeftHand;
            //         break;
            //     case "right-hand":
            //         partType = Hooks.Avatar.AvatarPartType.RightHand;
            //         break;
            //     default:
            //         Melon<Program>.Logger.Warning($"Unknown transform part: {partName}");
            //         return;
            // }

            // Melon<Program>.Logger.Msg("Starting AvatarSetTransformCoroutine.");
            // MelonCoroutines.Start(AvatarSetTransformCoroutine(avatar, partType, payload));
        }

        private void AvatarSetTransformCoroutine(Hooks.Avatar avatar, string partName, byte[] payload)
        {
            // Melon<Program>.Logger.Msg("AvatarSetTransformCoroutine started.");

            // yield return null;

            // Transform transformData = new Transform();
            // TransformSerializer.BytesToTransform(payload, transformData);
            // avatar.SetPartTransform(partType, transformData.position, transformData.rotation);

            switch (partName)
            {
                case "head":
                    if (avatar.HeadTransform == null)
                    {
                        Melon<Program>.Logger.Warning("Avatar's head transform is null.");
                        return;
                    }
                    MelonCoroutines.Start(TransformSerializer.BytesToTransform(payload, avatar.HeadTransform));
                    break;
                case "left-hand":
                    if (avatar.LeftTransform == null)
                    {
                        Melon<Program>.Logger.Warning("Avatar's left hand transform is null.");
                        return;
                    }
                    MelonCoroutines.Start(TransformSerializer.BytesToTransform(payload, avatar.LeftTransform));
                    break;
                case "right-hand":
                    if (avatar.RightTransform == null)
                    {
                        Melon<Program>.Logger.Warning("Avatar's right hand transform is null.");
                        return;
                    }
                    MelonCoroutines.Start(TransformSerializer.BytesToTransform(payload, avatar.RightTransform));
                    break;
                default:
                    Melon<Program>.Logger.Warning($"Unknown transform part: {partName}");
                    return;
            }
        }

        private string ParsePlayerIdFromTopic(string topic)
        {
            string pattern = @"^player/([^/]+)/";
            Match match = Regex.Match(topic, pattern);
            string extracted = string.Empty;
            if (match.Success)
                extracted = match.Groups[1].Value;
            if (extracted != string.Empty)
                return extracted;

            Melon<Program>.Logger.Warning($"Failed to extract player name from topic: {topic}");
            return string.Empty;
        }

        private IEnumerator SendAvatarPosCoroutine(string playerId)
        {
            InstanceManager instanceManager = InstanceManager.Instance ??
                throw new NullReferenceException("InstanceManager instance is null in SendAvatarPosCoroutine!");
            MqttManager mqttManager = MqttManager.Instance
                ?? throw new NullReferenceException("MqttManager instance is null in SendAvatarPosCoroutine!");

            int frameCount = 0;

            yield return null;
            while (instanceManager.IsInInstance)
            {
                if (frameCount % 4 == 0)
                {
                    byte[] headTransformData = TransformSerializer.TransformToBytes(_headTransform);
                    byte[] leftHandTransformData = TransformSerializer.TransformToBytes(_leftHandTransform);
                    byte[] rightHandTransformData = TransformSerializer.TransformToBytes(_rightHandTransform);

                    _ = mqttManager.PublishAsync($"player/{playerId}/transform/head", headTransformData, 0, false);
                    _ = mqttManager.PublishAsync($"player/{playerId}/transform/left-hand", leftHandTransformData, 0, false);
                    _ = mqttManager.PublishAsync($"player/{playerId}/transform/right-hand", rightHandTransformData, 0, false);

                    frameCount = 0;
                }
                frameCount++;
                yield return null;
            }
        }
    }
}
