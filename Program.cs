using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
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
            DateTimeOffset dtStart = DateTimeOffset.Now;

            TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            TelemetryClient telemetryClient = new TelemetryClient(telemetryConfiguration);
            ConsoleLogger logger = new ConsoleLogger(telemetryClient);
            using PerformanceCollectorModule perfCollectorModule = new PerformanceCollectorModule(); // https://github.com/MicrosoftDocs/azure-docs/blob/master/articles/azure-monitor/app/performance-counters.md
            perfCollectorModule.Initialize(telemetryConfiguration);

            try
            {
                using DependencyTrackingTelemetryModule aiDependencyTrackingTelemetryModule = new DependencyTrackingTelemetryModule();
                aiDependencyTrackingTelemetryModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
                aiDependencyTrackingTelemetryModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
                aiDependencyTrackingTelemetryModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("127.0.0.1");
                aiDependencyTrackingTelemetryModule.Initialize(telemetryConfiguration);
                telemetryConfiguration.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());

                using FollowerBot bot = new FollowerBot(args, logger, telemetryClient);
                try
                {
                    bot.Run();

                    DateTimeOffset dtEnd = DateTimeOffset.Now;
                    telemetryClient.TrackAvailability(bot.BotUserEmail, dtEnd, (dtEnd - dtStart), Environment.MachineName, true);
                    ret = 0;
                }
                catch (Exception ex)
                {
                    DateTimeOffset dtEnd = DateTimeOffset.Now;

                    bot.DebugDump();

                    telemetryClient.TrackAvailability(bot.BotUserEmail, dtEnd, (dtEnd - dtStart), Environment.MachineName, false, ex.GetBaseException().Message);
                    throw;
                }
            }
            catch (FollowerBotException ex)
            {
                logger.LogCritical(default, ex, "## ENDED IN ERROR : {0}", ex.GetBaseException().Message);
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

    }
}
