namespace YuchiGames.POM.Hooks
{
    public static class AvatarFactory
    {
        private static List<Avatar> s_avatars = new List<Avatar>();

        public static Avatar CreateAvatar(string id)
        {
            Avatar avatar = new Avatar(id);
            s_avatars.Add(avatar);
            return avatar;
        }

        public static void DestroyAvatar(string id)
        {
            Avatar? avatar = s_avatars.FirstOrDefault(a => a.Id == id);
            if (avatar != null)
            {
                avatar.Destroy();
                s_avatars.Remove(avatar);
            }
        }

        public static void DestroyAvatar(Avatar avatar)
        {
            if (s_avatars.Contains(avatar))
            {
                avatar.Destroy();
                s_avatars.Remove(avatar);
            }
        }

        public static Avatar GetAvatar(string id)
        {
            Avatar? avatar = s_avatars.FirstOrDefault(a => a.Id == id);

            if (avatar != null)
            {
                return avatar;
            }
            else
            {
                throw new Exception($"Avatar with ID {id} not found.");
            }
        }
    }
}
