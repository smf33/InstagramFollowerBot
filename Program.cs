using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace InstagramFollowerBot
{
    public class Program
    {

        private static int Main(string[] args)
        {
            int ret;
            ConsoleLogger logger = new ConsoleLogger();

            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            TelemetryClient telemetryClient = new TelemetryClient(configuration);

            try
            {
                using DependencyTrackingTelemetryModule aiDependencyTrackingTelemetryModule = new DependencyTrackingTelemetryModule();
                aiDependencyTrackingTelemetryModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
                aiDependencyTrackingTelemetryModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
                aiDependencyTrackingTelemetryModule.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("127.0.0.1");
                aiDependencyTrackingTelemetryModule.Initialize(configuration);
                configuration.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());

                using FollowerBot bot = new FollowerBot(args, logger, telemetryClient);
                try
                {
                    bot.Run();
                }
                catch
                {
                    bot.DebugDump();
                    throw;
                }
                ret = 0;
            }
            catch (ApplicationException ex)
            {
                logger.LogCritical(default, ex, "## ENDED IN ERROR : {0}", ex.GetBaseException().Message);
                ret = -1;
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                logger.LogCritical(default, ex, "## ENDED IN ERROR : {0}", ex.GetBaseException().Message);
                ret = -2;
            }

            telemetryClient.Flush();
            Task.Delay(5000).Wait();

            return ret;
        }

    }
}
