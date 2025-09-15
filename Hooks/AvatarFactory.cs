using System.Collections;
using MelonLoader;

namespace YuchiGames.POM.Hooks
{
    /// <summary>
    /// アバターの生成、破棄、取得を行うためのメソッドを提供します。
    /// </summary>
    public static class AvatarFactory
    {
        /// <summary>
        /// すべてのアバターを取得します。
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, Avatar> AllAvatars => s_avatars;

        private static Dictionary<string, Avatar> s_avatars = new Dictionary<string, Avatar>();

        /// <summary>
        /// 指定したIDで新しいアバターを作成します。
        /// 同じIDのアバターが既に存在する場合はArgumentExceptionをスローします。
        /// </summary>
        /// <param name="id">アバターの一意な識別子。</param>
        /// <returns>作成されたアバターインスタンス。</returns>
        /// <exception cref="ArgumentException">同じIDのアバターが既に存在する場合にスローされます。</exception>
        public static IEnumerator Create(string id)
        {
            yield return null;

            if (s_avatars.ContainsKey(id))
                throw new ArgumentException($"Avatar with ID {id} already exists.");
            Avatar avatar = new Avatar(id);
            s_avatars[id] = avatar;
            Melon<Program>.Logger.Msg($"Created avatar with ID {id}");
        }

        /// <summary>
        /// 指定したIDのアバターを取得します。
        /// アバターが見つからない場合はKeyNotFoundExceptionをスローします。
        /// </summary>
        /// <param name="id">取得するアバターの一意な識別子。</param>
        /// <returns>指定したIDのアバターインスタンス。</returns>
        /// <exception cref="KeyNotFoundException">指定したIDのアバターが見つからない場合にスローされます。</exception>
        public static Avatar GetAvatar(string id)
        {
            if (!s_avatars.ContainsKey(id))
                throw new KeyNotFoundException($"Avatar with ID {id} not found.");
            Avatar avatar = s_avatars[id];
            return avatar;
        }

        /// <summary>
        /// 指定したIDのアバターを破棄します。
        /// </summary>
        /// <param name="id">破棄するアバターの一意な識別子。</param>
        public static IEnumerator Destroy(string id)
        {
            yield return null;

            if (!s_avatars.ContainsKey(id))
            {
                Melon<Program>.Logger.Warning($"Attempted to destroy non-existent avatar with ID {id}.");
                yield break;
            }
            Avatar avatar = s_avatars[id];
            avatar.Destroy();
            s_avatars.Remove(id);
        }

        /// <summary>
        /// 指定したアバターインスタンスを破棄します。
        /// </summary>
        /// <param name="avatar">破棄するアバターインスタンス。</param>
        public static IEnumerator Destroy(Avatar avatar)
        {
            yield return Destroy(avatar.Id);
        }
    }
}
