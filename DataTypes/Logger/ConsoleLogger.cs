using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YuchiGames.POM.Shared;

namespace Shared.Logger
{
    public class ConsoleLogger : StyledLogger
    {
        ConsoleColor LogColor(ILogger.LogLevel level) => level switch
        {
            ILogger.LogLevel.Info => ConsoleColor.White,
            ILogger.LogLevel.Debug => ConsoleColor.Gray,
            ILogger.LogLevel.Warning => ConsoleColor.Yellow,
            ILogger.LogLevel.Error => ConsoleColor.Red,
            _ => throw new NotImplementedException()
        };

        public override void DecorateLog(ILogger.LogLevel level, string mark)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(mark);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public override void InternalLog(ILogger.LogLevel level, string message)
        {
            Console.ForegroundColor = LogColor(level);
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
