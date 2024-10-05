using MelonLoader;
using UnityEngine;
using System.Text;
using Microsoft.Win32;
using System.Text.Json;
using System.Reflection;
using YuchiGames.POM.Shared;
using System.Runtime.Versioning;
using YuchiGames.POM.Client.Assets;
using YuchiGames.POM.Client.Managers;
using Il2CppInterop.Runtime.Injection;
using UnityEngine.Rendering.VirtualTexturing;
using Il2Cpp;

namespace YuchiGames.POM.Client
{
    public class Program : MelonMod
    {
        public static ClientSettings Settings { get; private set; } = new();
        public static string Version { get; private set; } = "";
        public static string UserGUID { get; private set; } = "";


        [SupportedOSPlatform("windows")]
        public override void OnInitializeMelon()
        {
            ClassInjector.RegisterTypeInIl2Cpp<GroupSyncerComponent>();

            string settingsPath = $"{Directory.GetCurrentDirectory()}/Mods/settings.json";

            Settings = ClientSettings.LoadFromFileOrCreateDefault(settingsPath);

            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? throw new Exception("Version not found.");

            using RegistryKey? key = 
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\SQMClient")
                ?? throw new Exception("SQMClient not found");

            string machineID = key.GetValue("MachineId")?.ToString()
                ?? throw new Exception("MachineId not found");

            UserGUID = machineID.Trim('{', '}');

            MelonEvents.OnSceneWasInitialized.Subscribe(PingUI.OnSceneWasInitialized);
            MelonEvents.OnSceneWasInitialized.Subscribe((_, _) => SystemObject.Init());
            MelonEvents.OnSceneWasInitialized.Subscribe((_, _) => Network.Init());
            MelonEvents.OnUpdate.Subscribe(GroupSyncerComponent.GlobalUpdate);
            MelonEvents.OnUpdate.Subscribe(Network.OnUpdate);
            MelonEvents.OnUpdate.Subscribe(PingUI.OnUpdate);
            MelonEvents.OnGUI.Subscribe(InfoGUI.OnGUI);
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                InfoGUI.IsShow = !InfoGUI.IsShow;
        }

        public override void OnApplicationQuit() =>
            Network.Disconnect();
    }
}
