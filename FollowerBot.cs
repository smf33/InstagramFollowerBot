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
        private readonly DependencyTrackingTelemetryModule aiDependencyTrackingTelemetryModule; // even if not used, must be kept during all the app life

        public FollowerBot(string[] configArgs, ILogger logger, TelemetryClient telemetryCli)
        {
            Log = logger;
            telemetryClient = telemetryCli;
            LoadConfig(configArgs);
            telemetryClient.Context.User.Id = Config.BotUserEmail;

            Log.LogInformation("## LOADING...");
            var opLoading = telemetryClient.StartOperation(new RequestTelemetry { Name = "LOADING", Url = new Uri(string.Concat(Config.UrlRoot, "?loading=", Config.BotUserEmail)) });
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
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);
                opLoading.Telemetry.Success = false;
                throw new ApplicationException("LOADING Exception", e);
            }
            finally
            {
                telemetryClient.StopOperation(opLoading);
            }
        }

        public void Run()
        {
            Log.LogInformation("## LOGGING...");
            var opLogging = telemetryClient.StartOperation(new RequestTelemetry { Name = "LOGGING", Url = new Uri(string.Concat(Config.UrlRoot, Config.UrlLogin, "?logging=", Config.BotUserEmail)) });
            try
            {
                if (Data.UserContactUrl == null || !TryAuthCookies())
                {
                    AuthLogin();
                }
                Log.LogInformation("Logged user :  {0}", Data.UserContactUrl);
                PostAuthInit();
                SaveData(); // save cookies at last
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);
                opLogging.Telemetry.Success = false;
                throw new ApplicationException("LOGGING Exception", e);
            }
            finally
            {
                telemetryClient.StopOperation(opLogging);
            }

            Log.LogInformation("## RUNNING...");
            var opRunning = telemetryClient.StartOperation(new RequestTelemetry { Name = string.Concat("RUNNING ", Config.BotTasks), Url = new Uri(string.Concat(Data.UserContactUrl, "?running=", Config.BotTasks)) });
            try
            {

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
                    var opTask = telemetryClient.StartOperation(new RequestTelemetry { Name = string.Concat("TASK ", curTask), Url = new Uri(string.Concat(Data.UserContactUrl, "?task=", curTask)) });
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
                                continue; // no save anyway
                            case LoopStartStr:
                                continue; // no save anyway
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
                    }
                    catch (Exception e)
                    {
                        telemetryClient.TrackException(e);
                        opTask.Telemetry.Success = false;
                        throw new ApplicationException(string.Concat(curTask, " Exception"), e);
                    }
                    finally
                    {
                        telemetryClient.StopOperation(opTask);
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
            }
            catch (ApplicationException)
            {
                opRunning.Telemetry.Success = false;
                throw;
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);
                opRunning.Telemetry.Success = false;
                throw new ApplicationException("RUNNING Exception", e);
            }
            finally
            {
                telemetryClient.StopOperation(opRunning);
            }

            Log.LogInformation("## ENDED OK");
        }

        internal void DebugDump()
        {
            StringBuilder dump = new StringBuilder();
            try
            {
                dump.AppendFormat("# Try Dump last page : {0} @ {1}\r\n", Selenium.Title, Selenium.Url);
                dump.Append(Selenium.CurrentPageSource); // this one may crash more probably
            }
            catch
            {
                // Not usefull because already in exception
            }

            if (dump.Length > 0)
            {
                Log.LogDebug(dump.ToString());
            }
            else
            {
                Log.LogDebug("# Couldn't dump last page context");
            }

            try
            {
                Log.LogDebug("# Try saving Data in order to avoid queue polution");
                SaveData();
            }
            catch
            {
                // Not usefull because already in exception
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
        #endregion

    }
}
