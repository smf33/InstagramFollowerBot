using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
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
            telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtStart, (DateTimeOffset.Now - dtStart), "LOADING", true);
        }

        public void Run()
        {
            Log.LogInformation("## LOGGING...");
            if (Data.UserContactUrl == null || !TryAuthCookies())
            {
                AuthLogin();
            }
            Log.LogInformation("Logged user :  {0}", Data.UserContactUrl);
            PostAuthInit();
            SaveData(); // save cookies at last

            Log.LogInformation("## RUNNING...");
            foreach (string curTask in GetTasks(Config.BotTasks, Config.BotSaveAfterEachAction, Config.BotSaveOnEnd, Config.BotSaveOnLoop, Config.BotLoopTaskLimit))
            {
                Log.LogInformation("# {0}...", curTask);
                DateTimeOffset dtStart = DateTimeOffset.Now;
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
                    telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtStart, (DateTimeOffset.Now - dtStart), curTask, true);
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackAvailability(string.Concat(Environment.MachineName, '@', Config.BotUserEmail), dtStart, (DateTimeOffset.Now - dtStart), curTask, false, ex.GetBaseException().Message);
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