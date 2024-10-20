using MelonLoader;

namespace YuchiGames.POM.Server
{
    public class Settings
    {
        public int Port { get; }
        public int MaxPlayers { get; }

        private MelonPreferences_Category _pomCategory;
        private MelonPreferences_Entry<int> _portEntry;
        private MelonPreferences_Entry<int> _maxPlayersEntry;

        public Settings()
        {
            _pomCategory = MelonPreferences.CreateCategory("PrimitierOnlineServerMod");
            _portEntry = _pomCategory.CreateEntry("Port", 46491);
            _portEntry.Description = "The port the server will listen on.";
            _maxPlayersEntry = _pomCategory.CreateEntry("MaxPlayers", 16);
            _maxPlayersEntry.Description = "The maximum number of players that can connect to the server.";

            if (_portEntry.Value < 1024 || 49151 < _portEntry.Value)
            {
                Melon<Program>.Logger.Warning("Port is out of range, using default port 46491.");
                _portEntry.ResetToDefault();
            }
            if (_maxPlayersEntry.Value < 1)
            {
                Melon<Program>.Logger.Warning("Max players is out of range, using default max players 16.");
                _maxPlayersEntry.ResetToDefault();
            }
            MelonPreferences.Save();

            Port = _portEntry.Value;
            MaxPlayers = _maxPlayersEntry.Value;
        }
    }
}
