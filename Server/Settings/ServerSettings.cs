using Newtonsoft.Json;

namespace YuchiGames.POM.Server
{
    public class ServerSettings
    {
        public bool DayNightCycle { get; set; } = true;
        public int MaxPlayers { get; set; } = 16;
        public int Port { get; set; } = 54162;
        public int Seed { get; set; } = 0;

        public float WorldUpdateDistance = 100f;
        public float WorldQuickUpdateDistance = 10f;

        [JsonIgnore]
        public string Version { get; set; } = "";
    }
}