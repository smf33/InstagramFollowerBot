using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace InstagramFollowerBot
{
    internal class ConsoleLogger : ILogger
    {
        private readonly TelemetryClient telemetryClient;

        public ConsoleLogger(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

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
            string msg = formatter(state, exception);
            switch (logLevel)
            {
                case LogLevel.Critical:
                    Log(ConsoleColor.Magenta, msg, ConsoleColor.DarkMagenta, exception);
                    telemetryClient.TrackTrace(msg, SeverityLevel.Critical);
                    break;

                case LogLevel.Error:
                    Log(ConsoleColor.Red, msg, ConsoleColor.DarkRed, exception);
                    telemetryClient.TrackTrace(msg, SeverityLevel.Error);
                    break;

                case LogLevel.Warning:
                    Log(ConsoleColor.Yellow, msg, ConsoleColor.DarkYellow, exception);
                    telemetryClient.TrackTrace(msg, SeverityLevel.Warning);
                    break;

                case LogLevel.Information:
                    Log(ConsoleColor.White, msg, 0, exception);
                    telemetryClient.TrackTrace(msg, SeverityLevel.Information);
                    break;

                default:
                    Log(ConsoleColor.DarkGray, msg, 0, exception);
                    telemetryClient.TrackTrace(msg, SeverityLevel.Verbose);
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