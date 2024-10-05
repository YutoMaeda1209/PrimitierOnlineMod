using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YuchiGames.POM.Shared
{
    public interface ILogger
    {
        public enum LogLevel
        {
            Debug, Info, Warning, Error
        }

        public void Log(LogLevel level, string message);

        public void Debug(string message) =>
            Log(LogLevel.Debug, message);
        public void Info(string message) =>
            Log(LogLevel.Info, message);
        public void Warning(string message) =>
            Log(LogLevel.Warning, message);
        public void Error(string message) => 
            Log(LogLevel.Error, message);
    }

    public class EmptyLogger : ILogger
    {
        public void Log(ILogger.LogLevel level, string message) { }
    }
}
