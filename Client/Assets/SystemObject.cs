using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace YuchiGames.POM.Client.Assets
{
    public class SystemObject : MonoBehaviour
    {
        public static GameObject s_systemObject = null!;
        public static CubeGenerator s_cubeGenerator = null!;

        public static void Init()
        {
            s_systemObject = GameObject.Find("System");
            s_cubeGenerator = s_systemObject.GetComponent<CubeGenerator>();
        }
    }
}
