using MelonLoader;

namespace YuchiGames.POM.Client
{
    public class Settings
    {
        public string IPAddress
        {
            get => _ipAddressEntry?.Value ?? "127.0.0.1";
        }
        public int Port
        {
            get => _portEntry?.Value ?? 54162;
        }

        private MelonPreferences_Category _pomCategory;
        private MelonPreferences_Entry<string> _ipAddressEntry;
        private MelonPreferences_Entry<int> _portEntry;

        public Settings()
        {
            _pomCategory = MelonPreferences.CreateCategory("PrimitierOnlineServerMod");
            _ipAddressEntry = _pomCategory.CreateEntry("IPAddress", "127.0.0.1");
            _ipAddressEntry.Description = "The IP address of the server.";
            _portEntry = _pomCategory.CreateEntry("Port", 54162);
            _portEntry.Description = "The port the server will listen on.";
        }
    }
}
