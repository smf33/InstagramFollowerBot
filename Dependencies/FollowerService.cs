using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        private string _UserName;

        private string UserName
        {
            get
            {
                if (string.IsNullOrEmpty(_UserName))
                {
                    _UserName = _serviceProvider
                        .GetRequiredService<IOptions<LoggingOptions>>()
                        .Value
                        .User;
                }
                return _UserName;
            }
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            // load actions to do
            Queue<string> taskQueue = new Queue<string>(_serviceProvider.GetRequiredService<TaskLoader>().GetTaskNameList());
            TelemetryClient telemetryClient = _serviceProvider.GetService<TelemetryClient>(); // may be null

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
                        action = _serviceProvider.GetRequiredService<HomeAction>();
                        break;

                    case "DOEXPLOREPHOTOSFOLLOWLIKE":
                    case "DOEXPLOREPHOTOSLIKEFOLLOW":
                        action = _serviceProvider.GetRequiredService<ExplorePhotosAction>();
                        break;

                    case "DOEXPLOREPHOTOSLIKE":
                        action = _serviceProvider.GetRequiredService<ExplorePhotosAction>();
                        ((IFollowAction)action).DoFollow = false;
                        break;

                    case "DOEXPLOREPHOTOSFOLLOW":
                        action = _serviceProvider.GetRequiredService<ExplorePhotosAction>();
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

                _logger.LogInformation("{0}", curTask);

                DateTimeOffset dtStart = DateTimeOffset.Now;
                try
                {
                    await action
                        .RunAsync();
                    if (telemetryClient != null)
                    {
                        telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', UserName), dtStart, (DateTimeOffset.Now - dtStart), curTask, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{0} EXCEPTION : {1}", curTask, ex.GetBaseException().Message);
                    if (telemetryClient != null)
                    {
                        telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', UserName), dtStart, (DateTimeOffset.Now - dtStart), curTask, false, ex.GetBaseException().Message);
                        telemetryClient.TrackException(ex);
                    }
                    // dump png and html if required
                    _serviceProvider.GetRequiredService<PersistenceAction>()
                        .DumpCurrentPageIfRequired();
                    throw;
                }
            }

            // flush telemetry
            if (telemetryClient != null)
            {
                telemetryClient.Flush();
                await Task.Delay(3000);
            }
        }
    }
}