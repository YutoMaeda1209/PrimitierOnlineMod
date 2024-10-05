using Il2Cpp;
using YuchiGames.POM.Shared.DataObjects;
using UnityEngine;
using YuchiGames.POM.Shared;
using Unity.XR.CoreUtils;

namespace YuchiGames.POM.Client.Managers
{
    public class Player
    {
        private GameObject _playerObject;
        private Transform[] _playerObjectTransforms;

        private static GameObject s_basePlayerObject;
        public static Dictionary<int, Player> ConnectedPlayers { get; set; } = new();
        public static Transform[] s_clientPlayerTransform;

        static Player()
        {
            AvatarVisibility player = GameObject.FindObjectOfType<AvatarVisibility>();
            s_basePlayerObject = LoadBasePlayerObject(player);
            s_clientPlayerTransform = GetClientPlayerTransform(player, GameObject.FindObjectOfType<XROrigin>());
        }

        static Transform[] GetClientPlayerTransform(AvatarVisibility player, XROrigin origin)
        {
            return new[] { player.proxyHead.transform, player.proxyLeftHand.transform, player.proxyRightHand.transform, origin.transform };
        }

        static GameObject LoadBasePlayerObject(AvatarVisibility basePlayer)
        {
            var basePlayerObject = new GameObject("BaseBody");
            basePlayerObject.active = false;
            basePlayerObject.transform.position = Vector3.zero;

            GameObject s_headObject = GameObject.Instantiate(basePlayer.proxyHead);
            s_headObject.name = "Head";
            s_headObject.transform.parent = basePlayerObject.transform;

            GameObject s_leftHandObject = GameObject.Instantiate(basePlayer.proxyLeftHand);
            GameObject.Destroy(s_leftHandObject.GetComponent<ProxyFingerController>());
            s_leftHandObject.name = "LeftHand";
            s_leftHandObject.transform.parent = basePlayerObject.transform;

            GameObject s_rightHandObject = GameObject.Instantiate(basePlayer.proxyRightHand);
            GameObject.Destroy(s_rightHandObject.GetComponent<ProxyFingerController>());
            s_rightHandObject.name = "RightHand";
            s_rightHandObject.transform.parent = basePlayerObject.transform;

            return basePlayerObject;
        }

        private Player(int id)
        {
            _playerObjectTransforms = new Transform[3];

            _playerObject = GameObject.Instantiate(s_basePlayerObject);
            _playerObject.name = $"BaseBody_Player{id}";
            _playerObject.active = true;

            _playerObjectTransforms[0] = _playerObject.transform.Find("Head");
            _playerObjectTransforms[1] = _playerObject.transform.Find("LeftHand");
            _playerObjectTransforms[2] = _playerObject.transform.Find("RightHand");

            // Self:
            /*_playerObjectTransforms[0] = avatarVisibility.proxyHead.transform;
            _playerObjectTransforms[1] = avatarVisibility.proxyLeftHand.transform;
            _playerObjectTransforms[2] = avatarVisibility.proxyRightHand.transform;*/
        }

        private void Destroy()
        {
            GameObject.Destroy(_playerObject);
        }

        public static void SpawnPlayer(int id)
        {
            ConnectedPlayers[id] = new Player(id);
        }

        public static void DespawnPlayer(int id)
        {
            ConnectedPlayers[id].Destroy();
            ConnectedPlayers.Remove(id);
        }

        public PlayerPositionData GetPositionData() => new()
        {
            Head = _playerObjectTransforms[0].ToShared(),
            LeftHand = _playerObjectTransforms[1].ToShared(),
            RightHand = _playerObjectTransforms[2].ToShared(),
        };

        public static PlayerPositionData GetLocalPlayerPositionData() => new()
        {
            BasePosition = s_clientPlayerTransform[3].transform.position.ToShared(),
            Head = s_clientPlayerTransform[0].transform.ToShared(),
            LeftHand = s_clientPlayerTransform[1].transform.ToShared(),
            RightHand = s_clientPlayerTransform[2].transform.ToShared(),
        };

        public SVector3 PlayerLastPositionData = new(0, 0, 0);

        public void SetPositionData(PlayerPositionData posData)
        {
            PlayerLastPositionData = posData.BasePosition;

            _playerObjectTransforms[0].position = DataConverter.ToUnity(posData.Head.Position);
            _playerObjectTransforms[0].rotation = DataConverter.ToUnity(posData.Head.Rotation);
            _playerObjectTransforms[1].position = DataConverter.ToUnity(posData.LeftHand.Position);
            _playerObjectTransforms[1].rotation = DataConverter.ToUnity(posData.LeftHand.Rotation);
            _playerObjectTransforms[2].position = DataConverter.ToUnity(posData.RightHand.Position);
            _playerObjectTransforms[2].rotation = DataConverter.ToUnity(posData.RightHand.Rotation);
        }
    }
}