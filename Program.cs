using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.Extensions.Logging;

namespace InstagramFollowerBot
{
    public class Program
    {
        private static int Main(string[] args)
        {
            int ret = -1;

            TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            TelemetryClient telemetryClient = new TelemetryClient(telemetryConfiguration);
            ConsoleLogger logger = new ConsoleLogger(telemetryClient);
            using PerformanceCollectorModule perfCollectorModule = new PerformanceCollectorModule(); // https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/azure-monitor/app/performance-counters.md
            perfCollectorModule.Initialize(telemetryConfiguration);

            try
            {
                using FollowerBot bot = new FollowerBot(args, logger, telemetryClient);

                try
                {
                    bot.Run();
                    ret = 0;
                }
                catch
                {
                    bot.DebugDump();
                    throw;
                }
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                logger.LogCritical(default, ex, "## ENDED IN ERROR : {0}", ex.GetBaseException().Message);
            }

            telemetryClient.Flush();
            Task.Delay(5000).Wait();

            return ret;
        }

        // Utility classes should not have public constructors
        protected Program()
        {
        }
    }
}