using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace IFB
{
    internal class VsoLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, VsoLogger> _loggers = new ConcurrentDictionary<string, VsoLogger>();
        private bool disposedValue;

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new VsoLogger(categoryName));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _loggers.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}