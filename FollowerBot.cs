using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace InstagramFollowerBot
{
    public partial class FollowerBot : IDisposable
    {
        private const string DetectContactsFollowBackStr = "DETECTCONTACTSFOLLOWBACK";
        private const string DetectContactsUnfollowBackStr = "DETECTCONTACTSUNFOLLOWBACK";
        private const string DoContactsFollowStr = "DOCONTACTSFOLLOW";
        private const string DoContactsUnfollowStr = "DOCONTACTSUNFOLLOW";
        private const string DoHomePhotosLikeStr = "DOHOMEPHOTOSLIKE";
        private const string DoPhotosLikeStr = "DOPHOTOSLIKE";
        private const string DoPhotosLikeJustFollowStr = "DOPHOTOSLIKE_FOLLOWONLY";
        private const string DoPhotosLikeJustLikeStr = "DOPHOTOSLIKE_LIKEONLY";
        private const string ExplorePhotosStr = "EXPLOREPHOTOS";
        private const string ExplorePeopleSuggestedStr = "EXPLOREPEOPLESUGGESTED";
        private const string PauseStr = "PAUSE";
        private const string SearchKeywordsStr = "SEARCHKEYWORDS";
        private const string SaveStr = "SAVE";
        private const string WaitStr = "WAIT";

        private static readonly string ExecPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private readonly ILogger Log;

        public FollowerBot(string[] configArgs, ILogger logger)
        {
            Log = logger;

            Log.LogInformation("## LOADING...");
            LoadConfig(configArgs);

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
        }

        public void Run()
        {
            Log.LogInformation("## LOGGING...");

            if (Data.UserContactUrl == null
                || !TryAuthCookies())
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

                    case DoHomePhotosLikeStr:
                        DoHomePhotosLike();
                        break;

                    case PauseStr:
                    case WaitStr:
                        Task.Delay(PseudoRand.Next(Config.BotWaitTaskMinWaitMs, Config.BotWaitTaskMaxWaitMs))
                            .Wait();
                        continue; // no save anyway

                    default:
                        Log.LogError("Unknown BotTask : {0}", curTask);
                        break;
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