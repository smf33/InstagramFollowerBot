using System;
using Microsoft.Extensions.Logging;

namespace IFB
{
    internal class VsoLogger : ILogger
    {
        private readonly string _categoryName;

        public VsoLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    Log("error", formatter(state, exception), exception);
                    break;

                case LogLevel.Warning:
                    Log("warning", formatter(state, exception), exception);
                    break;

                case LogLevel.Information:
                    Log(null, formatter(state, exception), exception);
                    break;

                case LogLevel.Debug:
                case LogLevel.Trace:
                    Log("debug", formatter(state, exception), exception);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void Log(string prefixVso, string message, Exception exception)
        {
            // VSO prefix
            if (!string.IsNullOrEmpty(prefixVso))
            {
                Console.Write("##[{0}]", prefixVso);
            }
            // message itsef
            Console.WriteLine("[{0}] {1}", _categoryName, message);
            // more detail
            if (exception != null)
            {
                Console.WriteLine("##[group]Exception");
                Console.WriteLine(exception);
                Console.WriteLine("##[endgroup]");
            }
        }
    }
}