using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using YuchiGames.POM.Client.Managers;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace YuchiGames.POM.Client.Assets
{
    public class StartButton : MonoBehaviour
    {
        public static bool IsInteractable
        {
            get
            {
                if (s_button is null)
                    return false;
                return s_button.interactable;
            }
            set
            {
                if (s_button is null)
                    return;
                s_button.interactable = value;
            }
        }

        private static Button? s_button;

        public static void Initialize()
        {
            GameObject startButton = GameObject.Find("/TitleSpace/TitleMenu/MainCanvas/StartButton");
            Destroy(startButton.GetComponent<ObjectActivateButton>());
            s_button = startButton.GetComponent<Button>();
            s_button.onClick.AddListener((UnityAction)OnClick);
        }

        private static void OnClick()
        {
            if (s_button is not null)
                s_button.interactable = false;
            Network.Connect(Program.Settings.IP, Program.Settings.Port);
        }

        public static void JoinGame()
        {
            TextMeshPro infoText = GameObject.Find("InfoText").GetComponent<TextMeshPro>();

            Il2CppReferenceArray<GameObject> destroyObjects = new Il2CppReferenceArray<GameObject>(1);
            destroyObjects[0] = GameObject.Find("/TitleSpace");

            Il2CppReferenceArray<GameObject> enableObjects = new Il2CppReferenceArray<GameObject>(2);
            GameObject systemTabObject = GameObject.Find("/Player/XR Origin/Camera Offset/LeftHand Controller/RealLeftHand/MenuWindowL/Windows/MainCanvas/SystemTab");
            enableObjects[0] = systemTabObject.transform.Find("DieButton").gameObject;
            enableObjects[1] = systemTabObject.transform.Find("BlueprintButton").gameObject;

            LoadingSequence loadingSequence = GameObject.FindObjectOfType<LoadingSequence>();
            //SaveAndLoad.Save(9); // TODO: save??
            loadingSequence.StartLoading(9, infoText, destroyObjects, enableObjects);
            Log.Information("Joined game successfully!");
        }
    }
}