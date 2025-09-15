using System.Collections;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace YuchiGames.POM.Hooks
{
    public class Avatar
    {
        public string Id { get { return _id; } }
        public Transform? AvatarTransform { get { return _avatarTransform; } }
        public Transform? HeadTransform { get { return _headTransform; } }
        public Transform? LeftTransform { get { return _leftTransform; } }
        public Transform? RightTransform { get { return _rightTransform; } }

        private static GameObject s_proxyHeadObject;
        private static GameObject s_proxyLeftObject;
        private static GameObject s_proxyRightObject;

        private string _id;
        private GameObject? _avatarObject;
        private GameObject? _headObject;
        private GameObject? _leftObject;
        private GameObject? _rightObject;
        private Transform? _avatarTransform;
        private Transform? _headTransform;
        private Transform? _leftTransform;
        private Transform? _rightTransform;

        static Avatar()
        {
            AvatarVisibility avatarVisibility = GameObject.FindObjectOfType<AvatarVisibility>();
            if (avatarVisibility != null)
            {
                s_proxyHeadObject = GameObject.Instantiate(avatarVisibility.proxyHead);
                s_proxyHeadObject.name = "ProxyHead";
                s_proxyLeftObject = GameObject.Instantiate(avatarVisibility.proxyLeftHand);
                s_proxyLeftObject.name = "ProxyLeftHand";
                s_proxyRightObject = GameObject.Instantiate(avatarVisibility.proxyRightHand);
                s_proxyRightObject.name = "ProxyRightHand";
            }
            else
            {
                throw new NullReferenceException("AvatarVisibility not found in the scene.");
            }
        }

        public Avatar(string id)
        {
            _id = id;
            _avatarObject = new GameObject($"Avatar_{id}");
            _avatarTransform = _avatarObject.transform;

            _headObject = GameObject.Instantiate(s_proxyHeadObject);
            _headObject.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");
            _headObject.transform.SetParent(_avatarObject.transform, false);
            _headTransform = _headObject.transform;

            _leftObject = GameObject.Instantiate(s_proxyLeftObject);
            GameObject.Destroy(_leftObject.GetComponent<ProxyFingerController>());
            _leftObject.transform.SetParent(_avatarObject.transform, false);
            _leftTransform = _leftObject.transform;

            _rightObject = GameObject.Instantiate(s_proxyRightObject);
            GameObject.Destroy(_rightObject.GetComponent<ProxyFingerController>());
            _rightObject.transform.SetParent(_avatarObject.transform, false);
            _rightTransform = _rightObject.transform;
        }

        public enum AvatarPartType
        {
            Head,
            LeftHand,
            RightHand
        }

        public void SetPartTransform(AvatarPartType avatarPartType, Vector3 position, Quaternion rotation)
        {
            Melon<Program>.Logger.Msg("SetPartTransform called.");

            switch (avatarPartType)
            {
                case AvatarPartType.Head:
                    if (_avatarObject == null)
                        throw new NullReferenceException("Avatar object is null.");
                    if (_headObject == null)
                        throw new NullReferenceException("Head object is null.");
                    _avatarObject.transform.position = position;
                    _headObject.transform.position = position;
                    _headObject.transform.rotation = rotation;
                    break;
                case AvatarPartType.LeftHand:
                    if (_leftObject == null)
                        throw new NullReferenceException("Left hand object is null.");
                    _leftObject.transform.position = position;
                    _leftObject.transform.rotation = rotation;
                    break;
                case AvatarPartType.RightHand:
                    if (_rightObject == null)
                        throw new NullReferenceException("Right hand object is null.");
                    _rightObject.transform.position = position;
                    _rightObject.transform.rotation = rotation;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(avatarPartType), avatarPartType, "Invalid avatar part type.");
            }
        }

        public void Destroy()
        {
            GameObject.Destroy(_headObject);
            _headObject = null;
            GameObject.Destroy(_leftObject);
            _leftObject = null;
            GameObject.Destroy(_rightObject);
            _rightObject = null;
            GameObject.Destroy(_avatarObject);
            _avatarObject = null;
        }
    }
}
