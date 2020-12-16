using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace InstagramFollowerBot
{
    public partial class FollowerBot : IDisposable
    {
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
            DateTimeOffset dtStart = DateTimeOffset.Now;
            Microsoft.ApplicationInsights.Extensibility.IOperationHolder<RequestTelemetry> opLoading = telemetryClient.StartOperation(new RequestTelemetry { Name = string.Concat("LOADING ", Config.BotUserEmail), Url = new Uri(string.Concat(Config.UrlRoot, "?loading=", Config.BotUserEmail)) });
            try
            {
                LoadData();

                string w = PseudoRand.Next(Config.SeleniumWindowMinW, Config.SeleniumWindowMaxW).ToString(CultureInfo.InvariantCulture);
                string h = PseudoRand.Next(Config.SeleniumWindowMinH, Config.SeleniumWindowMaxH).ToString(CultureInfo.InvariantCulture);
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
                DateTimeOffset dtEnd = DateTimeOffset.Now;
                telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtEnd, (dtEnd - dtStart), "LOADING", true);
            }
            catch (Exception e)
            {
                DateTimeOffset dtEnd = DateTimeOffset.Now;
                telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtEnd, (dtEnd - dtStart), "LOADING", false, e.GetBaseException().Message);
                telemetryClient.TrackException(e);
                opLoading.Telemetry.Success = false;
                telemetryClient.StopOperation(opLoading);
                throw new FollowerBotException("LOADING Exception", e);
            }
        }

        public void Run()
        {
            Log.LogInformation("## LOGGING...");
            DateTimeOffset dtStart = DateTimeOffset.Now;
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
                DateTimeOffset dtEnd = DateTimeOffset.Now;
                telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtEnd, (dtEnd - dtStart), "LOGGING", true);
            }
            catch (Exception e)
            {
                DateTimeOffset dtEnd = DateTimeOffset.Now;
                telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtEnd, (dtEnd - dtStart), "LOGGING", false, e.GetBaseException().Message);
                telemetryClient.TrackException(e);
                opLogging.Telemetry.Success = false;
                telemetryClient.StopOperation(opLogging);
                throw new FollowerBotException("LOGGING Exception", e);
            }

            Log.LogInformation("## RUNNING...");
            telemetryClient.TrackPageView(Config.BotTasks);
            foreach (string curTask in GetTasks(Config.BotTasks, Config.BotSaveAfterEachAction, Config.BotSaveOnEnd, Config.BotSaveOnLoop, Config.BotLoopTaskLimit))
            {
                Log.LogInformation("# {0}...", curTask);
                Microsoft.ApplicationInsights.Extensibility.IOperationHolder<RequestTelemetry> opTask = telemetryClient.StartOperation(new RequestTelemetry { Name = string.Concat(curTask, " ", Config.BotUserEmail), Url = new Uri(string.Concat(Data.UserContactUrl, "?task=", curTask)) });
                dtStart = DateTimeOffset.Now;
                try
                {
                    switch (curTask)
                    {
                        case "DETECTCONTACTSFOLLOWBACK":
                            DetectContactsFollowBack();
                            break;

                        case "DETECTCONTACTSUNFOLLOWBACK":
                            DetectContactsUnfollowBack();
                            break;

                        case "DOCONTACTSFOLLOW":
                            DoContactsFollow();
                            break;

                        case "DOCONTACTSUNFOLLOW":
                            DoContactsUnfollow();
                            break;

                        case "DOPHOTOSLIKE":
                            DoPhotosLike(true, true);
                            break;

                        case "DOPHOTOSLIKE_FOLLOWONLY":
                            DoPhotosLike(true, false);
                            break;

                        case "DOPHOTOSLIKE_LIKEONLY":
                            DoPhotosLike(false, true);
                            break;

                        case "EXPLOREPHOTOS":
                            ExplorePhotos();
                            break;

                        case "EXPLOREPEOPLESUGGESTED":
                            ExplorePeople();
                            break;

                        case "SAVE":
                            SaveData();
                            break;

                        case "SEARCHKEYWORDS":
                            SearchKeywords();
                            break;

                        case "DOHOMEPAGELIKE":
                            DoHomePageActions();
                            break;

                        case "DOEXPLOREPHOTOSLIKEFOLLOW":
                            DoExplorePhotosPageActions(true, true);
                            break;

                        case "DOEXPLOREPHOTOSLIKE":
                            DoExplorePhotosPageActions(true, false);
                            break;

                        case "DOEXPLOREPHOTOSFOLLOW":
                            DoExplorePhotosPageActions(false, true);
                            break;

                        case "PAUSE":
                        case "WAIT":
                            Task.Delay(PseudoRand.Next(Config.BotWaitTaskMinWaitMs, Config.BotWaitTaskMaxWaitMs))
                                .Wait();
                            break;

                        default:
                            Log.LogError("Unknown BotTask : {0}", curTask);
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
            }

            Log.LogInformation("## ENDED OK");
        }

        internal void DebugDump()
        {
            try
            {
                Log.LogDebug("# Try saving Data in order to avoid queue polution");
                SaveData();
            }
            catch
            {
                // Not usefull because already in exception context
            }
            finally
            {
                try
                {
                    Log.LogDebug("# Try to dumping last page : {0} @ {1}\r\n", Selenium.Title, Selenium.Url);
                    Selenium.DumpCurrentPage(Config.BotUserSaveFolder ?? ExecPath, Config.BotUserEmail);
                }
                catch
                {
                    // Not usefull because already in exception context
                }
            }
        }
    }
}