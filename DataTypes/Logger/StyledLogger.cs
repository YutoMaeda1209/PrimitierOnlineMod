using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YuchiGames.POM.Shared;

namespace Shared.Logger
{
    public abstract class StyledLogger : ILogger
    {
        char LogLetter(ILogger.LogLevel logLevel) => logLevel switch
        {
            ILogger.LogLevel.Info => 'I',
            ILogger.LogLevel.Debug => 'D',
            ILogger.LogLevel.Error => 'E',
            ILogger.LogLevel.Warning => 'W',
            _ => throw new NotImplementedException()
        };

        public void Log(ILogger.LogLevel level, string message)
        {
            DecorateLog(level, $"[{DateTime.Now.ToString("HH:mm:ss.fff")} {LogLetter(level)}] ");
            InternalLog(level, $"{message}");
        }


        public abstract void DecorateLog(ILogger.LogLevel level, string mark);
        public abstract void InternalLog(ILogger.LogLevel level, string message);
    }
}
