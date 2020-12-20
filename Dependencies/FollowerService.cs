using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IFB
{
    internal class FollowerService : IBotAction
    {
        private readonly ILogger<FollowerService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FollowerService(ILogger<FollowerService> logger, IServiceProvider serviceProvider) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new FollowerService()");
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            // load actions to do
            Queue<string> taskQueue = new Queue<string>(_serviceProvider.GetRequiredService<TaskLoader>().GetTaskNameList());

            // just do it
            while (taskQueue.TryDequeue(out string curTask))
            {
                IBotAction action;
                switch (curTask)
                {
                    case "LOADING":
                        action = _serviceProvider.GetRequiredService<SeleniumWrapper>();
                        break;

                    case "LOGGING":
                        action = _serviceProvider.GetRequiredService<LoggingAction>();
                        break;

                    case "CHECKACTIVITY":
                        action = _serviceProvider.GetRequiredService<ActivityAction>();
                        break;

                    case "SAVE":
                        action = _serviceProvider.GetRequiredService<PersistenceAction>();
                        break;

                    case "DOHOMEPAGELIKE":
                        action = _serviceProvider.GetRequiredService<HomePageAction>();
                        break;

                    case "DOEXPLOREPHOTOSFOLLOWLIKE":
                    case "DOEXPLOREPHOTOSLIKEFOLLOW":
                        action = _serviceProvider.GetRequiredService<ExplorePhotosPageActions>();
                        break;

                    case "DOEXPLOREPHOTOSLIKE":
                        action = _serviceProvider.GetRequiredService<ExplorePhotosPageActions>();
                        ((IFollowAction)action).DoFollow = false;
                        break;

                    case "DOEXPLOREPHOTOSFOLLOW":
                        action = _serviceProvider.GetRequiredService<ExplorePhotosPageActions>();
                        ((ILikeAction)action).DoLike = false;
                        break;

                    case "PAUSE":
                    case "WAIT":
                        action = _serviceProvider.GetRequiredService<WaitAction>();
                        break;

                    default:
                        _logger.LogWarning("Unknown Task : {0}", curTask);
                        continue;
                }

                _logger.LogInformation("{0}...", curTask);

                DateTimeOffset dtStart = DateTimeOffset.Now;
                try
                {
                    await action
                        .RunAsync();
                    telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtStart, (DateTimeOffset.Now - dtStart), curTask, true);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtStart, (DateTimeOffset.Now - dtStart), curTask, false, ex.GetBaseException().Message);
                    throw;
                }
            }
        }
    }
}