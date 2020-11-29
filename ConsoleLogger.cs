using System;
using Microsoft.Extensions.Logging;

namespace InstagramFollowerBot
{
    internal class ConsoleLogger : ILogger
    {
        private static void Log(ConsoleColor color, string message, ConsoleColor debugColor, object debug)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            if (debug != null)
            {
                Console.ForegroundColor = debugColor;
                Console.WriteLine(debug);
            }
            Console.ResetColor();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    Log(ConsoleColor.Red, formatter(state, exception), ConsoleColor.DarkRed, exception);
                    break;

                case LogLevel.Warning:
                    Log(ConsoleColor.Yellow, formatter(state, exception), ConsoleColor.DarkYellow, exception);
                    break;

                case LogLevel.Information:
                    Log(ConsoleColor.White, formatter(state, exception), 0, exception);
                    break;

                default:
                    Log(ConsoleColor.DarkGray, formatter(state, exception), 0, exception);
                    break;
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