using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YuchiGames.POM.Shared;

namespace Shared.Logger
{
    public class MultipleLoggerManager : ILogger
    {
        public MultipleLoggerManager() { }
        public MultipleLoggerManager(params ILogger[] loggers)
        {
            Loggers = loggers.ToList();
        }

        public List<ILogger> Loggers { get; set; } = new();

        public void Log(ILogger.LogLevel level, string message) =>
            Loggers.ForEach(logger => logger.Log(level, message));
    }
}
