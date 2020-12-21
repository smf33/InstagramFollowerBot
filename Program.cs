using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace IFB
{
    public class Program
    {
        // Utility classes should not have public constructors
        protected Program()
        {
        }

        public static async Task<int> Main(string[] args)
        {
            int ret;
            IServiceProvider serviceProvider = null;
            try
            {
                // Setup
                serviceProvider = ConfigureServices(args);

                // Run
                await serviceProvider
                    .GetRequiredService<FollowerService>()
                    .RunAsync();

                ret = 0;
            }
            catch
            {
                ret = -1;
            }

            // CleanUp
            if (serviceProvider is IDisposable serviceProviderDisposable)
            {
                serviceProviderDisposable.Dispose();
            }

            // End the application
            return ret;
        }

        private static IServiceProvider ConfigureServices(string[] args)
        {
            // setup config
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Files.ExecutablePath)
                .AddJsonFile("InstagramFollowerBot.json", optional: false, reloadOnChange: false) // priority 4
                .AddEnvironmentVariables() // priority 2 prefix: "IFB_"
                .AddCommandLine(args) // priority 1
                .Build();

            // init services
            IServiceCollection services = new ServiceCollection();

            // Setup Logging
            LoggerOptions loggerOptions = new LoggerOptions();
            configuration.GetSection(LoggerOptions.Section).Bind(loggerOptions);
            if (loggerOptions.UseApplicationInsights)
            {
                services.AddApplicationInsightsTelemetryWorkerService(new ApplicationInsightsServiceOptions()
                {
                    EnableDiagnosticsTelemetryModule = false, // disable Microsoft telemetric : EnableHeartbeat, EnableAzureInstanceMetadataTelemetryModule, EnableAppServicesHeartbeatTelemetryModule
                    EnableQuickPulseMetricStream = false, // LiveMetrics
                    EnableAdaptiveSampling = false
                });
                // remove spam and useless module
                ServiceDescriptor module = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(DependencyTrackingTelemetryModule));
                if (module != null)
                {
                    services.Remove(module);
                }
                module = services.FirstOrDefault<ServiceDescriptor>(t => t.ImplementationType == typeof(QuickPulseTelemetryModule));
                if (module != null)
                {
                    services.Remove(module);
                }
            }
            services.AddLogging(configure =>
            {
                configure.SetMinimumLevel(loggerOptions.MinimumLevel);
                if (!loggerOptions.UseAzureDevOpsFormating)
                {
                    configure.AddProvider(new ColoredConsoleLoggerProvider());
                }
                else
                {
                    configure.AddProvider(new VsoLoggerProvider());
                }
                if (loggerOptions.UseApplicationInsights)
                {
                    configure.AddApplicationInsights() // test normaly useless : exception aren t reported currently
                        .AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, loggerOptions.MinimumLevel); // else Information by default
                }
            });

            // Add the applications services
            return services
                .AddOptions()
                .AddSingleton<PersistenceAction>() // must be singleton
                .AddSingleton<SeleniumWrapper>() // must be singleton
                .AddTransient<ActivityAction>() // can be used one or more time
                .AddTransient<ExplorePhotosAction>() // can be used one or more time
                .AddTransient<FollowerService>() // used once
                .AddTransient<HomeAction>() // can be used one or more time
                .AddTransient<LoggingAction>() // used once
                .AddTransient<TaskLoader>() // used once
                .AddTransient<WaitAction>() // used few time
                .Configure<ExplorePhotosOptions>(configuration.GetSection(ExplorePhotosOptions.Section))
                .Configure<HomePageOptions>(configuration.GetSection(HomePageOptions.Section))
                .Configure<InstagramOptions>(configuration.GetSection(InstagramOptions.Section))
                .Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.Section))
                .Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.Section))
                .Configure<SeleniumOptions>(configuration.GetSection(SeleniumOptions.Section))
                .Configure<TaskManagerOptions>(configuration.GetSection(TaskManagerOptions.Section))
                .Configure<WaitOptions>(configuration.GetSection(WaitOptions.Section))
                .BuildServiceProvider();
        }
    }
}