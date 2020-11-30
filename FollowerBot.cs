using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.Extensions.Logging;

namespace InstagramFollowerBot
{
    public partial class FollowerBot : IDisposable
    {
        private const string DetectContactsFollowBackStr = "DETECTCONTACTSFOLLOWBACK";
        private const string DetectContactsUnfollowBackStr = "DETECTCONTACTSUNFOLLOWBACK";
        private const string DoContactsFollowStr = "DOCONTACTSFOLLOW";
        private const string DoContactsUnfollowStr = "DOCONTACTSUNFOLLOW";
        private const string DoPhotosLikeStr = "DOPHOTOSLIKE";
        private const string DoPhotosLikeJustFollowStr = "DOPHOTOSLIKE_FOLLOWONLY";
        private const string DoPhotosLikeJustLikeStr = "DOPHOTOSLIKE_LIKEONLY";
        private const string ExplorePhotosStr = "EXPLOREPHOTOS";
        private const string ExplorePeopleSuggestedStr = "EXPLOREPEOPLESUGGESTED";
        private const string LoopStartStr = "LOOPSTART";
        private const string LoopStr = "LOOP";
        private const string PauseStr = "PAUSE";
        private const string SearchKeywordsStr = "SEARCHKEYWORDS";
        private const string SaveStr = "SAVE";
        private const string WaitStr = "WAIT";

        private static readonly string ExecPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private readonly ILogger Log;
        private readonly TelemetryClient telemetryClient;

        public FollowerBot(string[] configArgs, ILogger logger, TelemetryClient telemetryCli)
        {
            Log = logger;
            telemetryClient = telemetryCli;
            LoadConfig(configArgs);
            telemetryClient.Context.User.Id = Config.BotUserEmail;

            Log.LogInformation("## LOADING...");
            Microsoft.ApplicationInsights.Extensibility.IOperationHolder<RequestTelemetry> opLoading = telemetryClient.StartOperation(new RequestTelemetry { Name = string.Concat("LOADING ", Config.BotUserEmail), Url = new Uri(string.Concat(Config.UrlRoot, "?loading=", Config.BotUserEmail)) });
            try
            {
                LoadData();

                string w = Rand.Next(Config.SeleniumWindowMinW, Config.SeleniumWindowMaxW).ToString(CultureInfo.InvariantCulture);
                string h = Rand.Next(Config.SeleniumWindowMinH, Config.SeleniumWindowMaxH).ToString(CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(Config.SeleniumRemoteServer))
                {
                    Log.LogDebug("NewChromeSeleniumWrapper({0}, {1}, {2})", ExecPath, w, h);
                    Selenium = SeleniumWrapper.NewChromeSeleniumWrapper(ExecPath, w, h, Config.SeleniumBrowserArguments, Config.BotSeleniumTimeoutSec);
                }
                else
                {
                    if (Config.SeleniumRemoteServerWarmUpWaitMs > 0)
                    {
                        Task.Delay(Config.SeleniumRemoteServerWarmUpWaitMs)
                            .Wait();
                    }
                    Log.LogDebug("NewRemoteSeleniumWrapper({0}, {1}, {2})", Config.SeleniumRemoteServer, w, h);
                    Selenium = SeleniumWrapper.NewRemoteSeleniumWrapper(Config.SeleniumRemoteServer, w, h, Config.SeleniumBrowserArguments, Config.BotSeleniumTimeoutSec);
                }
                telemetryClient.StopOperation(opLoading);
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);
                opLoading.Telemetry.Success = false;
                telemetryClient.StopOperation(opLoading);
                throw new FollowerBotException("LOADING Exception", e);
            }
        }

        public void Run()
        {
            Log.LogInformation("## LOGGING...");
            Microsoft.ApplicationInsights.Extensibility.IOperationHolder<RequestTelemetry> opLogging = telemetryClient.StartOperation(new RequestTelemetry { Name = string.Concat("LOGGING ", Config.BotUserEmail), Url = new Uri(string.Concat(Config.UrlRoot, Config.UrlLogin, "?logging=", Config.BotUserEmail)) });
            try
            {
                if (Data.UserContactUrl == null || !TryAuthCookies())
                {
                    AuthLogin();
                }
                Log.LogInformation("Logged user :  {0}", Data.UserContactUrl);
                PostAuthInit();
                SaveData(); // save cookies at last
                telemetryClient.StopOperation(opLogging);
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);
                opLogging.Telemetry.Success = false;
                telemetryClient.StopOperation(opLogging);
                throw new FollowerBotException("LOGGING Exception", e);
            }

            Log.LogInformation("## RUNNING...");
            telemetryClient.TrackPageView(Config.BotTasks);
            string[] tasks = Config.BotTasks.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = tasks[i].Trim().ToUpperInvariant(); // standardize
            }
            int iLoop = Config.BotLoopTaskLimited;
            for (int i = 0; i < tasks.Length; i++)
            {
                string curTask = tasks[i];
                Log.LogInformation("# {0}...", curTask);
                Microsoft.ApplicationInsights.Extensibility.IOperationHolder<RequestTelemetry> opTask = telemetryClient.StartOperation(new RequestTelemetry { Name = string.Concat(curTask, " ", Config.BotUserEmail), Url = new Uri(string.Concat(Data.UserContactUrl, "?task=", curTask)) });
                DateTimeOffset dtStart = DateTimeOffset.Now;
                try
                {
                    switch (curTask)
                    {
                        case DetectContactsFollowBackStr:
                            DetectContactsFollowBack();
                            break;

                        case DetectContactsUnfollowBackStr:
                            DetectContactsUnfollowBack();
                            break;

                        case DoContactsFollowStr:
                            DoContactsFollow();
                            break;

                        case DoContactsUnfollowStr:
                            DoContactsUnfollow();
                            break;

                        case DoPhotosLikeStr:
                            DoPhotosLike();
                            break;

                        case DoPhotosLikeJustFollowStr:
                            DoPhotosLike(true, false);
                            break;

                        case DoPhotosLikeJustLikeStr:
                            DoPhotosLike(false, true);
                            break;

                        case ExplorePhotosStr:
                            ExplorePhotos();
                            break;

                        case ExplorePeopleSuggestedStr:
                            ExplorePeopleSuggested();
                            break;

                        case SaveStr: // managed in the if after
                            break;

                        case SearchKeywordsStr:
                            SearchKeywords();
                            break;

                        case PauseStr:
                        case WaitStr:
                            Task.Delay(Rand.Next(Config.BotWaitTaskMinWaitSec, Config.BotWaitTaskMaxWaitSec))
                                .Wait();
                            break;

                        case LoopStartStr:
                            break;

                        case LoopStr:
                            if (Config.BotLoopTaskLimited <= 0)
                            {
                                i = Array.IndexOf(tasks, LoopStartStr); // -1 (so ok) if not found
                            }
                            else if (iLoop > 0)
                            {
                                Log.LogDebug("Loop still todo : {0}", iLoop);
                                iLoop--;
                                i = Array.IndexOf(tasks, LoopStartStr); // -1 (so ok) if not found
                            }
                            if (Config.BotSaveOnLoop)
                            {
                                curTask = SaveStr;
                            }
                            break;

                        default:
                            Log.LogError("Unknown BotTask : {0}", tasks[i]);
                            break;
                    }
                    telemetryClient.StopOperation(opTask);
                    DateTimeOffset dtEnd = DateTimeOffset.Now;
                    telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtEnd, (dtEnd - dtStart), curTask, true);
                }
                catch (FollowerBotException ex)
                {
                    DateTimeOffset dtEnd = DateTimeOffset.Now;
                    telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtEnd, (dtEnd - dtStart), curTask, false, ex.GetBaseException().Message);
                    opTask.Telemetry.Success = false;
                    telemetryClient.StopOperation(opTask);
                    throw;
                }
                catch (Exception ex)
                {
                    DateTimeOffset dtEnd = DateTimeOffset.Now;
                    telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtEnd, (dtEnd - dtStart), curTask, false, ex.GetBaseException().Message);
                    telemetryClient.TrackException(ex);
                    opTask.Telemetry.Success = false;
                    telemetryClient.StopOperation(opTask);
                    throw new FollowerBotException(string.Concat(curTask, " Exception"), ex);
                }

                if (Config.BotSaveAfterEachAction || curTask == SaveStr)
                {
                    SaveData();
                }
            }
            if (Config.BotSaveOnEnd)
            {
                SaveData();
            }

            Log.LogInformation("## ENDED OK");
        }

        internal void DebugDump()
        {
            try
            {
                Log.LogInformation("Try saving Data in order to avoid queue polution");
                SaveData();

                Log.LogInformation("##[group]Dumping last page : {0} @ {1}\r\n", Selenium.Title, Selenium.Url);
                Log.LogInformation(Selenium.CurrentPageSource); // this one may crash more probably
                Log.LogInformation("##[endgroup]");
            }
            catch (Exception ex)
            {
                Log.LogWarning(default, ex, "DebugDump() Failed : {0}", ex.GetBaseException().Message);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls
        private SeleniumWrapper Selenium;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Selenium.Dispose();
                    Selenium = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}