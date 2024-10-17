using UnityEngine;

namespace YuchiGames.POM.Client.Assets
{
    public abstract class Asset : MonoBehaviour
    {
        public abstract GameObject Object { get; }
        public abstract void Create();
    }
}
