using Newtonsoft.Json;

namespace YuchiGames.POM.Server
{
    //TODO: Add log settings THERE
    public class ServerConfig : ServerSettings
    {
        public static ServerConfig? Load(string path) =>
            JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText(path));

        public static void Save(ServerConfig config) =>
            JsonConvert.SerializeObject(config, Formatting.Indented);
    }
}
