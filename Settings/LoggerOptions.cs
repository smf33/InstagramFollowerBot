using Microsoft.Extensions.Logging;

namespace IFB
{
    internal class LoggerOptions
    {
        internal const string Section = "IFB_Logger";

        // used
        public LogLevel MinimumLevel { get; set; }

        public bool UseAzureDevOpsFormating { get; set; }
        public bool UseApplicationInsights { get; set; }
    }
}