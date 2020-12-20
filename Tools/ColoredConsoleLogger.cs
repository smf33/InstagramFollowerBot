using System;
using Microsoft.Extensions.Logging;

namespace IFB
{
    internal class ColoredConsoleLogger : ILogger
    {
        private readonly string _categoryName;

        public ColoredConsoleLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        private void Log(string lvlName, ConsoleColor color, string message, ConsoleColor exceptionColor, Exception exception)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("[{0}:{1}] {2}", lvlName, _categoryName, message);
            if (exception != null)
            {
                Console.ForegroundColor = exceptionColor;
                Console.WriteLine(exception);
            }
            Console.ResetColor();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    Log("CRI", ConsoleColor.Red, formatter(state, exception), ConsoleColor.DarkRed, exception);
                    break;

                case LogLevel.Error:
                    Log("ERR", ConsoleColor.Magenta, formatter(state, exception), ConsoleColor.DarkMagenta, exception);
                    break;

                case LogLevel.Warning:
                    Log("WRN", ConsoleColor.Yellow, formatter(state, exception), ConsoleColor.DarkYellow, exception);
                    break;

                case LogLevel.Information:
                    Log("INF", ConsoleColor.White, formatter(state, exception), ConsoleColor.Gray, exception);
                    break;

                case LogLevel.Debug:
                    Log("DBG", ConsoleColor.Gray, formatter(state, exception), ConsoleColor.DarkGray, exception);
                    break;

                case LogLevel.Trace:
                    Log("TRA", ConsoleColor.DarkGray, formatter(state, exception), ConsoleColor.DarkGray, exception);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }
}