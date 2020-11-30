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

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string msg = formatter(state, exception);
            switch (logLevel)
            {
                case LogLevel.Critical:
                    Console.WriteLine(string.Concat("##[error]", msg));
                    if (exception != null)
                    {
                        Console.WriteLine("##[group]Exception");
                        Console.WriteLine(exception);
                        Console.WriteLine("##[endgroup]");
                    }
                    telemetryClient.TrackTrace(msg, SeverityLevel.Critical);
                    break;

                case LogLevel.Error:
                    Console.WriteLine(string.Concat("##[error]", msg));
                    if (exception != null)
                    {
                        Console.WriteLine("##[group]Exception");
                        Console.WriteLine(exception);
                        Console.WriteLine("##[endgroup]");
                    }
                    telemetryClient.TrackTrace(msg, SeverityLevel.Error);
                    break;

                case LogLevel.Warning:
                    Console.WriteLine(string.Concat("##[warning]", msg));
                    if (exception != null)
                    {
                        Console.WriteLine("##[group]Exception");
                        Console.WriteLine(exception);
                        Console.WriteLine("##[endgroup]");
                    }
                    telemetryClient.TrackTrace(msg, SeverityLevel.Warning);
                    break;

                case LogLevel.Information:
                    Console.WriteLine(msg);
                    if (exception != null)
                    {
                        Console.WriteLine("##[group]Exception");
                        Console.WriteLine(exception);
                        Console.WriteLine("##[endgroup]");
                    }
                    telemetryClient.TrackTrace(msg, SeverityLevel.Information);
                    break;

                default:
                    Console.WriteLine(string.Concat("##[debug]", msg));
                    if (exception != null)
                    {
                        Console.WriteLine("##[group]Exception");
                        Console.WriteLine(exception);
                        Console.WriteLine("##[endgroup]");
                    }
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