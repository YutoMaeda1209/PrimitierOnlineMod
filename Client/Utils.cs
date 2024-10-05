using Il2CppInterop.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using YuchiGames.POM.Shared.Utils;

namespace Client
{
    public static class UnityUtils
    {
        /// <summary>
        /// Allows to find even inactive objects
        /// </summary>
        public static GameObject FindGameObject(Func<GameObject, bool> predicate) =>
            GameObject.FindObjectsOfTypeAll(Il2CppType.Of<Transform>())
            .Select(obj => obj.Cast<Transform>().gameObject)
            .First(predicate);
        /// <summary>
        /// Allows to find even inactive objects
        /// </summary>
        public static GameObject FindGameObject(string name) =>
            FindGameObject(go => go.name == name);

        public static T FindGameObjectOfType<T>() where T : UnityEngine.Object =>
            FindGameObject(obj => obj.GetComponent<T>() != null).GetComponent<T>();

        public static IEnumerable<Transform> Childrens(this Transform transform)
        {
            for (var childId = 0; childId < transform.GetChildCount(); childId++)
                yield return transform.GetChild(childId);
        }

        public static IEnumerable<GameObject> ChildrensObjects(this Transform transform)
        {
            for (var childId = 0; childId < transform.GetChildCount(); childId++)
                yield return transform.GetChild(childId).gameObject;
        }

        public static int ChildIndex(this Transform transform) =>
            transform.parent.Childrens().IndexOf(transform);
    }

    public static class Il2CppUtils
    {
        public delegate Il2CppSystem.Object? InvokeFunction(params Il2CppSystem.Object[] args);
        public static InvokeFunction PrivateMethod(this Il2CppSystem.Object sourceObjects, string methodName)
        {
            var action = sourceObjects.GetIl2CppType().GetMethod(methodName);
            Il2CppSystem.Object Fn(params Il2CppSystem.Object[] args) =>
                action.Invoke(sourceObjects, args);
            return Fn;
        }
    }
}
