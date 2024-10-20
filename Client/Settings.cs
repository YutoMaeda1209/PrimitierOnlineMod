using MelonLoader;
using System.Net;

namespace YuchiGames.POM.Client
{
    public class Settings
    {
        public string Address { get; }
        public int Port { get; }

        private MelonPreferences_Category _pomCategory;
        private MelonPreferences_Entry<string> _serverAddressEntry;

        public Settings()
        {
            _pomCategory = MelonPreferences.CreateCategory("PrimitierOnlineMod");
            _serverAddressEntry = _pomCategory.CreateEntry("ServerAddress", "127.0.0.1");
            _serverAddressEntry.Description = "The address of the server to connect to.";

            string[] validParts = _serverAddressEntry.Value.Split(':');
            if (!IPAddress.TryParse(validParts[0], out _))
            {
                Melon<Program>.Logger.Warning("IPAddress is invalid, using default server address 127.0.0.1.");
                _serverAddressEntry.ResetToDefault();
            }
            if (validParts.Length == 2)
            {
                int tmpPort = 0;
                if (!int.TryParse(validParts[1], out tmpPort))
                {
                    Melon<Program>.Logger.Warning("Port number is invalid, using default server address 127.0.0.1.");
                    _serverAddressEntry.ResetToDefault();
                }
                else if (tmpPort < 1024 || 49151 < tmpPort)
                {
                    Melon<Program>.Logger.Warning("Port is out of range, using default server address 127.0.0.1.");
                    _serverAddressEntry.ResetToDefault();
                }
            }
            if (2 < validParts.Length)
            {
                Melon<Program>.Logger.Warning("Server address is invalid, using default server address 127.0.0.1.");
                _serverAddressEntry.ResetToDefault();
            }
            MelonPreferences.Save();

            string[] splitParts = _serverAddressEntry.Value.Split(':');
            Address = splitParts[0];
            Port = splitParts.Length == 2 ? int.Parse(splitParts[1]) : 46491;
        }
    }
}
