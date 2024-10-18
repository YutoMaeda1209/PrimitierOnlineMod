using MelonLoader;

namespace YuchiGames.POM.Server
{
    public class Settings
    {
        public int Port
        {
            get => _portEntry?.Value ?? 54162;
        }
        public int MaxPlayers
        {
            get => _maxPlayersEntry?.Value ?? 16;
        }

        private MelonPreferences_Category _pomCategory;
        private MelonPreferences_Entry<int> _portEntry;
        private MelonPreferences_Entry<int> _maxPlayersEntry;

        public Settings()
        {
            _pomCategory = MelonPreferences.CreateCategory("PrimitierOnlineServerMod");
            _portEntry = _pomCategory.CreateEntry("Port", 54162);
            _portEntry.Description = "The port the server will listen on.";
            _maxPlayersEntry = _pomCategory.CreateEntry("MaxPlayers", 16);
            _maxPlayersEntry.Description = "The maximum number of players that can connect to the server.";
        }
    }
}
