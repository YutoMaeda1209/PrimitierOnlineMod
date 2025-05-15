using Il2Cpp;
using UnityEngine;

namespace YuchiGames.POM.Hooks
{
    public class Avatar
    {
        private static GameObject s_proxyHeadObject;
        private static GameObject s_proxyLeftObject;
        private static GameObject s_proxyRightObject;

        private string _id;
        private GameObject _avatarObject;
        private GameObject _headObject;
        private GameObject _leftObject;
        private GameObject _rightObject;

        public string Id { get { return _id; } }

        static Avatar()
        {
            AvatarVisibility avatarVisibility = GameObject.FindObjectOfType<AvatarVisibility>();
            if (avatarVisibility != null)
            {
                s_proxyHeadObject = avatarVisibility.proxyHead;
                s_proxyHeadObject.name = "ProxyHead";
                s_proxyLeftObject = avatarVisibility.proxyLeftHand;
                s_proxyLeftObject.name = "ProxyLeftHand";
                s_proxyRightObject = avatarVisibility.proxyRightHand;
                s_proxyRightObject.name = "ProxyRightHand";
            }
            else
            {
                throw new Exception("AvatarVisibility not found in the scene.");
            }
        }

        public Avatar(string id)
        {
            _id = id;
            _avatarObject = new GameObject($"Avatar_{id}");

            _headObject = GameObject.Instantiate(s_proxyHeadObject);
            _headObject.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");
            _headObject.transform.SetParent(_avatarObject.transform, false);

            _leftObject = GameObject.Instantiate(s_proxyLeftObject);
            GameObject.Destroy(_leftObject.GetComponent<ProxyFingerController>());
            _leftObject.transform.SetParent(_avatarObject.transform, false);

            _rightObject = GameObject.Instantiate(s_proxyRightObject);
            GameObject.Destroy(_rightObject.GetComponent<ProxyFingerController>());
            _rightObject.transform.SetParent(_avatarObject.transform, false);
        }

        public void SetTransform(Transform transform)
        {
            _avatarObject.transform.position = transform.position;
            _avatarObject.transform.rotation = transform.rotation;
        }

        public void SetHeadTransform(Transform transform)
        {
            _headObject.transform.position = transform.position;
            _headObject.transform.rotation = transform.rotation;
        }

        public void SetLeftTransform(Transform transform)
        {
            _leftObject.transform.position = transform.position;
            _leftObject.transform.rotation = transform.rotation;
        }

        public void SetRightTransform(Transform transform)
        {
            _rightObject.transform.position = transform.position;
            _rightObject.transform.rotation = transform.rotation;
        }

        public void Destroy()
        {
            GameObject.Destroy(_headObject);
            GameObject.Destroy(_leftObject);
            GameObject.Destroy(_rightObject);
            GameObject.Destroy(_avatarObject);
        }
    }
}
