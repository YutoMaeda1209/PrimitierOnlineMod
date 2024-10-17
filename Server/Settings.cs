using MelonLoader;

namespace YuchiGames.POM.Server
{
    public static class Settings
    {
        public static int Port
        {
            get => s_portEntry?.Value ?? 54162;
        }
        public static int MaxPlayers
        {
            get => s_maxPlayersEntry?.Value ?? 16;
        }

        private static MelonPreferences_Category? s_pomCategory;
        private static MelonPreferences_Entry<int>? s_portEntry;
        private static MelonPreferences_Entry<int>? s_maxPlayersEntry;

        public static void Initialize()
        {
            s_pomCategory = MelonPreferences.CreateCategory("PrimitierOnlineServerMod");
            s_portEntry = s_pomCategory.CreateEntry("Port", 54162);
            s_portEntry.Description = "The port the server will listen on.";
            s_maxPlayersEntry = s_pomCategory.CreateEntry("MaxPlayers", 16);
            s_maxPlayersEntry.Description = "The maximum number of players that can connect to the server.";
        }
    }
}
