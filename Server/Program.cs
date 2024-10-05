using Shared.Logger;
using System.Reflection;
using YuchiGames.POM.Server.Managers.CommandSystem;
using YuchiGames.POM.Shared.Utils;

namespace YuchiGames.POM.Server
{
    static class Program
    {
        private static void Main()
        {
            string configPath = "./settings.json";
            var serverConfig = ServerConfig.Load(configPath) ?? new ServerConfig().Apply(conf => POM.Server.ServerConfig.Save(conf));
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? throw new Exception("Version didn't founded!");

            serverConfig.Version = version;

            var server = new ServerApp(serverConfig);

            server.Log = new MultipleLoggerManager(new ConsoleLogger()); // Add file logger

            var commandManager = new CommandManager()
            {
                new Command("stop", args => server.Stop())
            };
            commandManager.Log = server.Log;
            
            commandManager.Start();
            server.Start().Wait();
        }
    }
}