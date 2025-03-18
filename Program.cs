using MelonLoader;
using System;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Client;
using System.Threading;
using System.Security.Authentication;
using Microsoft.Extensions.Configuration;
using UnityEngine;
using Il2Cpp;



namespace YuchiGames.POM
{
    public class Program : MelonMod
    {
        public override void OnInitializeMelon()
        {
            
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                int hoge = TerrainGenerator.worldSeed;
            }
        }

    }
}
