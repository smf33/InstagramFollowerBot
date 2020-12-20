using System;
using System.Threading.Tasks;
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

        public static async Task Main(string[] args)
        {
            // Setup
            IServiceProvider serviceProvider = ConfigureServices(args);

            // Run
            await serviceProvider.GetRequiredService<FollowerService>()
                .RunAsync();

            // CleanUp
            if (serviceProvider is IDisposable serviceProviderDisposable)
            {
                serviceProviderDisposable.Dispose();
            }
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
                .AddLogging(configure => configure
                    .AddConfiguration(configuration.GetSection("Logging"))
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