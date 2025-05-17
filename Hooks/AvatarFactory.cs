namespace YuchiGames.POM.Hooks
{
    public static class AvatarFactory
    {
        private static Dictionary<string, Avatar> s_avatars = new Dictionary<string, Avatar>();

        public static Avatar Create(string id)
        {
            if (s_avatars.ContainsKey(id))
                throw new ArgumentException($"Avatar with ID {id} already exists.");
            Avatar avatar = new Avatar(id);
            s_avatars[id] = avatar;
            return avatar;
        }

        public static void Destroy(string id)
        {
            if (!s_avatars.ContainsKey(id))
                throw new KeyNotFoundException($"Avatar with ID {id} not found.");
            Avatar avatar = s_avatars[id];
            avatar.Destroy();
            s_avatars.Remove(id);
        }

        public static void Destroy(Avatar avatar)
        {
            Destroy(avatar.Id);
        }

        public static Avatar GetAvatar(string id)
        {
            if (!s_avatars.ContainsKey(id))
                throw new KeyNotFoundException($"Avatar with ID {id} not found.");
            Avatar avatar = s_avatars[id];
            return avatar;
        }
    }
}
