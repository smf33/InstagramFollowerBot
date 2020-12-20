using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            // Setup
            int ret = 0;
            IServiceProvider serviceProvider = ConfigureServices(args);

            // telemetryClient
            TelemetryClient telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();

            // telemetryClient
            ILogger _logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Run
            try
            {
                await serviceProvider.GetRequiredService<FollowerService>()
                    .RunAsync();
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                _logger.LogCritical(ex, "EXCEPTION : {0}", ex.GetBaseException().Message);
                ret = -1;
            }

            // CleanUp
            if (serviceProvider is IDisposable serviceProviderDisposable)
            {
                serviceProviderDisposable.Dispose();
            }

            // flush telemetry
            telemetryClient.Flush();
            await Task.Delay(3000);

            // End the application
            return ret;
        }

        private static IServiceProvider ConfigureServices(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Files.ExecutablePath)
                .AddJsonFile("InstagramFollowerBot.json", optional: false, reloadOnChange: false) // priority 4
                .AddEnvironmentVariables() // priority 2 prefix: "IFB_"
                .AddCommandLine(args) // priority 1
                .Build();

            return new ServiceCollection()
                //.AddApplicationInsightsTelemetry()
                .AddLogging(configure => configure
                    .AddConfiguration(configuration.GetSection("Logging"))
                    .SetMinimumLevel(LogLevel
                    .AddProvider(new ColoredConsoleLoggerProvider()))
                .AddOptions()
                .AddSingleton<PersistenceAction>() // must be singleton
                .AddSingleton<SeleniumWrapper>() // must be singleton
                .AddTransient<ActivityAction>() // can be used one or more time
                .AddTransient<ExplorePhotosPageActions>() // can be used one or more time
                .AddTransient<FollowerService>() // used once
                .AddTransient<HomePageAction>() // can be used one or more time
                .AddTransient<LoggingAction>() // used once
                .AddTransient<TaskLoader>() // used once
                .AddTransient<WaitAction>() // used few time
                .Configure<ExplorePhotosPageActionsOptions>(configuration.GetSection(ExplorePhotosPageActionsOptions.Section))
                .Configure<HomePageActionsOptions>(configuration.GetSection(HomePageActionsOptions.Section))
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