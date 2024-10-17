using MelonLoader;

namespace YuchiGames.POM.Client
{
    public static class Settings
    {
        public static string IPAddress
        {
            get => s_ipAddressEntry?.Value ?? "127.0.0.1";
        }
        public static int Port
        {
            get => s_portEntry?.Value ?? 54162;
        }

        private static MelonPreferences_Category? s_pomCategory;
        private static MelonPreferences_Entry<string>? s_ipAddressEntry;
        private static MelonPreferences_Entry<int>? s_portEntry;

        public static void Initialize()
        {
            s_pomCategory = MelonPreferences.CreateCategory("PrimitierOnlineMod");
            s_ipAddressEntry = s_pomCategory.CreateEntry("IPAddress", "49.212.130.240");
            s_ipAddressEntry.Description = "The IP address of the server you want to connect to.";
            s_portEntry = s_pomCategory.CreateEntry("Port", 54162);
            s_portEntry.Description = "The port of the server you want to connect to.";
        }
    }
}
