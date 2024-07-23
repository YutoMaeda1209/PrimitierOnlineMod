﻿using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace YuchiGames.POM.Client.Assets
{
    public class StartButton : MonoBehaviour
    {
        public static void Initialize()
        {
            GameObject startButton = GameObject.Find("StartButton");
            Destroy(startButton.GetComponent<ObjectActivateButton>());
            Button button = startButton.GetComponent<Button>();
            button.onClick.AddListener((UnityAction)OnClick);
        }

        public static void OnClick()
        {
            TextMeshPro infoText = GameObject.Find("InfoText").GetComponent<TextMeshPro>();

            Il2CppReferenceArray<GameObject> destroyObjects = new Il2CppReferenceArray<GameObject>(1);
            destroyObjects[0] = GameObject.Find("/TitleSpace");

            Il2CppReferenceArray<GameObject> enableObjects = new Il2CppReferenceArray<GameObject>(2);
            GameObject saveLoadObject = GameObject.Find("/Player/XR Origin/Camera Offset/LeftHand Controller/RealLeftHand/MenuWindowL/Windows/MainCanvas/SystemTab");
            enableObjects[0] = saveLoadObject.transform.Find("DieButton").gameObject;
            enableObjects[1] = saveLoadObject.transform.Find("BlueprintButton").gameObject;

            LoadingSequence loadingSequence = GameObject.FindObjectOfType<LoadingSequence>();
            loadingSequence.StartLoading(1, infoText, destroyObjects, enableObjects);
        }
    }
}