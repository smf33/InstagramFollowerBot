using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
        internal static readonly string ExecutablePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // allow calling the program from a remote dir

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

                // Run service
                await serviceProvider
                    .GetRequiredService<BotService>()
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

        private static IServiceCollection ConfigureLogger(LoggerOptions loggerOptions)
        {
            // init a new blank service collection
            IServiceCollection services = new ServiceCollection();

            // manage ApplicationInsights
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

            // manage classic logging
            return services.AddLogging(configure =>
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
        }

        private static IServiceProvider ConfigureServices(string[] args)
        {
            // init a new blank service collection
            return ConfigureSettings(args)
                // Add the applications services
                .AddSingleton<PersistenceManager>() // must be singleton
                .AddSingleton<SeleniumWrapper>() // must be singleton
                .AddTransient<ActivityAction>() // can be used one or more time
                .AddTransient<BotService>() // used once
                .AddTransient<DumpingAction>() // can be used one
                .AddTransient<ExplorePhotosAction>() // can be used one or more time
                .AddTransient<FollowBackAction>() // can be used one or more time
                .AddTransient<HomeAction>() // can be used one or more time
                .AddTransient<LoadingAction>() // used once
                .AddTransient<LoggingAction>() // used once
                .AddTransient<SaveAction>() // used multiple time
                .AddTransient<SnapshootAction>() // can be used 2 times
                .AddTransient<TaskLoader>() // used once
                .AddTransient<UnfollowUnfollowersAction>() // can be used one or more time
                .AddTransient<WaitAction>() // used few time
                .BuildServiceProvider();
        }

        private static IServiceCollection ConfigureSettings(string[] args)
        {
            // setup config
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(ExecutablePath)
                .AddJsonFile("InstagramFollowerBot.json", optional: false, reloadOnChange: false) // priority 4
                .AddEnvironmentVariables() // priority 2 prefix: "IFB_"
                .AddCommandLine(args) // priority 1
                .Build();

            // generate loader specific settings for logger
            LoggerOptions loggerOptions = new LoggerOptions();
            configuration.GetSection(LoggerOptions.Section).Bind(loggerOptions);

            // INIT service collection with logger
            return ConfigureLogger(loggerOptions)
                // add settings as IOptions
                .AddOptions()
                .Configure<DumpingOptions>(configuration.GetSection(DumpingOptions.Section))
                .Configure<ExplorePhotosOptions>(configuration.GetSection(ExplorePhotosOptions.Section))
                .Configure<FollowBackOptions>(configuration.GetSection(FollowBackOptions.Section))
                .Configure<HomePageOptions>(configuration.GetSection(HomePageOptions.Section))
                .Configure<InstagramOptions>(configuration.GetSection(InstagramOptions.Section))
                .Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.Section))
                .Configure<LoggingSecretOptions>(configuration.GetSection(LoggingSecretOptions.Section))
                .Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.Section))
                .Configure<SeleniumOptions>(configuration.GetSection(SeleniumOptions.Section))
                .Configure<SnapshootOptions>(configuration.GetSection(SnapshootOptions.Section))
                .Configure<TaskManagerOptions>(configuration.GetSection(TaskManagerOptions.Section))
                .Configure<UnfollowUnfollowersOptions>(configuration.GetSection(UnfollowUnfollowersOptions.Section))
                .Configure<WaitOptions>(configuration.GetSection(WaitOptions.Section));
        }
    }
}