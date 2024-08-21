﻿using Il2Cpp;
using UnityEngine;
using Il2CppUniGLTF;
using Il2CppUniHumanoid;

namespace YuchiGames.POM.Client.Managers
{
    public static class Avatar
    {
        private static RuntimeGltfInstance[] s_gltfInstances;
        private static Transform[,] s_avatarTransforms;

        static Avatar()
        {
            Log.Debug($"MaxPlayers: {Network.ServerInfo.MaxPlayers}");
            s_gltfInstances = new RuntimeGltfInstance[Network.ServerInfo.MaxPlayers];
            s_avatarTransforms = new Transform[Network.ServerInfo.MaxPlayers, 15];
            GameObject myAvatarObject = GameObject.Find("/Player/AvatarParent/VRM1");
            Humanoid myAvatar = myAvatarObject.GetComponent<Humanoid>();
            s_avatarTransforms[Network.ID, 0] = myAvatar.GetBoneTransform(HumanBodyBones.Head);
            s_avatarTransforms[Network.ID, 1] = myAvatar.GetBoneTransform(HumanBodyBones.Hips);
            s_avatarTransforms[Network.ID, 2] = myAvatar.GetBoneTransform(HumanBodyBones.Spine);
            s_avatarTransforms[Network.ID, 3] = myAvatar.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            s_avatarTransforms[Network.ID, 4] = myAvatar.GetBoneTransform(HumanBodyBones.RightUpperArm);
            s_avatarTransforms[Network.ID, 5] = myAvatar.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            s_avatarTransforms[Network.ID, 6] = myAvatar.GetBoneTransform(HumanBodyBones.RightLowerArm);
            s_avatarTransforms[Network.ID, 7] = myAvatar.GetBoneTransform(HumanBodyBones.LeftHand);
            s_avatarTransforms[Network.ID, 8] = myAvatar.GetBoneTransform(HumanBodyBones.RightHand);
            s_avatarTransforms[Network.ID, 9] = myAvatar.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            s_avatarTransforms[Network.ID, 10] = myAvatar.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            s_avatarTransforms[Network.ID, 11] = myAvatar.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            s_avatarTransforms[Network.ID, 12] = myAvatar.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            s_avatarTransforms[Network.ID, 13] = myAvatar.GetBoneTransform(HumanBodyBones.LeftFoot);
            s_avatarTransforms[Network.ID, 14] = myAvatar.GetBoneTransform(HumanBodyBones.RightFoot);
        }

        public static byte[] GetAvatar()
        {
            string path = VrmLoader.GetAvatarDirectory();
            string[] files = Directory.GetFiles(path, "*.vrm");
            byte[] bytes = File.ReadAllBytes(files[0]);

            byte[] front = new byte[5];
            byte[] back = new byte[5];
            Array.Copy(bytes, 0, front, 0, front.Length);
            Array.Copy(bytes, bytes.Length - 5, back, 0, back.Length);
            Log.Debug($"Front: {BitConverter.ToString(front)}, Back: {BitConverter.ToString(back)}");

            return bytes;
        }

        public static void LoadAvatar(int id, byte[] vrmData)
        {
            byte[] front = new byte[5];
            byte[] back = new byte[5];
            Array.Copy(vrmData, 0, front, 0, front.Length);
            Array.Copy(vrmData, vrmData.Length - 5, back, 0, back.Length);
            Log.Debug($"Front: {BitConverter.ToString(front)}, Back: {BitConverter.ToString(back)}");

            Log.Debug("aaaa");
            GltfData gltfData = new GlbBinaryParser(vrmData, "").Parse();
            Log.Debug("bbbb");
            Il2CppUniGLTF.ImporterContext loader = new Il2CppUniGLTF.ImporterContext(gltfData);
            Log.Debug("cccc");
            RuntimeGltfInstance instance = loader.Load();
            Log.Debug("dddd");
            instance.name = $"Player_{id}";
            Log.Debug("eeee");
            instance.EnableUpdateWhenOffscreen();
            Log.Debug("ffff");
            instance.ShowMeshes();
            Log.Debug("gggg");
            s_gltfInstances[id] = instance;
            Log.Debug($"Avatar loaded: {id}, {s_gltfInstances[id].name}");
        }

        public static void LoadAvatars(byte[][] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != null)
                    LoadAvatar(i, data[i]);
            }
        }

        public static void DestroyAvatar(int id)
        {
            GameObject.Destroy(s_gltfInstances[id]);
            s_gltfInstances[id] = new RuntimeGltfInstance();
            Log.Debug($"Avatar destroyed: {id}");
        }
    }
}