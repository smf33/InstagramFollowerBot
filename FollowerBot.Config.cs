using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace InstagramFollowerBot
{
    public partial class FollowerBot
    {
        private class Configuration
        {
            internal IEnumerable<string> SeleniumBrowserArguments;
            internal bool BotCacheMyContacts;
            internal bool BotSaveAfterEachAction;
            internal bool BotSaveOnEnd;
            internal bool BotSaveOnLoop;
            internal bool BotUsePersistence;
            internal float BotSeleniumTimeoutSec;
            internal int BotCacheTimeLimitHours;
            internal int BotFollowTaskBatchMaxLimit;
            internal int BotFollowTaskBatchMinLimit;
            internal int BotHomeLikeInitScrools;
            internal int BotKeepSomeUnfollowerContacts;
            internal int BotLoopTaskLimit;
            internal int BotExplorePhotosScrools;
            internal int BotExplorePeopleSuggestedScrools;
            internal int BotSearchScrools;
            internal int BotStepFollowMaxWaitMs;
            internal int BotStepFollowMinWaitMs;
            internal int BotStepLikeMaxWaitMs;
            internal int BotStepLikeMinWaitMs;
            internal int BotStepMaxWaitMs;
            internal int BotStepMinWaitMs;
            internal int BotUnfollowTaskBatchMaxLimit;
            internal int BotUnfollowTaskBatchMinLimit;
            internal int BotUsePersistenceLimitHours;
            internal int BotLikeTaskBatchMaxLimit;
            internal int BotLikeTaskBatchMinLimit;
            internal int BotWaitTaskMaxWaitMs;
            internal int BotWaitTaskMinWaitMs;
            internal int SeleniumRemoteServerWarmUpWaitMs;
            internal int SeleniumWindowMaxH;
            internal int SeleniumWindowMaxW;
            internal int SeleniumWindowMinH;
            internal int SeleniumWindowMinW;
            internal int BotHomeLikeTaskBatchMaxLimit;
            internal int BotHomeLikeTaskBatchMinLimit;
            internal string AddContactsToFollow;
            internal string AddPhotosToLike;
            internal string BotSearchKeywords;
            internal string BotTasks;
            internal string BotUserEmail;
            internal string BotUserPassword;
            internal string BotUserSaveFolder;
            internal string CssActionWarning;
            internal string CssContactFollow;
            internal string CssContactUnfollowButton;
            internal string CssContactUnfollowButtonAlt;
            internal string CssContactUnfollowConfirm;
            internal string CssContactsFollowers;
            internal string CssContactsFollowing;
            internal string CssContactsListScrollable;
            internal string CssCookiesWarning;
            internal string CssLoginEmail;
            internal string CssLoginMyself;
            internal string CssLoginPassword;
            internal string CssLoginSageInfo;
            internal string CssLoginUnusual;
            internal string CssLoginWarning;
            internal string CssExplorePhotos;
            internal string CssPhotoFollow;
            internal string CssPhotoLike;
            internal string CssSuggestedContact;
            internal string SeleniumRemoteServer;
            internal string UrlContacts;
            internal string UrlExplore;
            internal string UrlExplorePeopleSuggested;
            internal string UrlLogin;
            internal string UrlRoot;
            internal string UrlSearch;
        }

        private Configuration Config;

        private void LoadConfig(string[] args)
        {
            string configJsonPath = ExecPath + "/InstagramFollowerBot.json";
            if (File.Exists(configJsonPath))
            {
                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile(configJsonPath) // default app config
                    .AddEnvironmentVariables()
                    .AddCommandLine(args) // priority
                    .Build();

                Config = new Configuration
                {
                    AddContactsToFollow = config["AddContactsToFollow"],
                    AddPhotosToLike = config["AddPhotosToLike"],
                    BotSearchKeywords = config["BotSearchKeywords"],
                    BotTasks = config["BotTasks"],
                    BotUserEmail = config["BotUserEmail"],
                    BotUserPassword = config["BotUserPassword"],
                    BotUserSaveFolder = config["BotUserSaveFolder"],
                    CssActionWarning = config["CssActionWarning"],
                    CssContactFollow = config["CssContactFollow"],
                    CssContactUnfollowButton = config["CssContactUnfollowButton"],
                    CssContactUnfollowButtonAlt = config["CssContactUnfollowButtonAlt"],
                    CssContactUnfollowConfirm = config["CssContactUnfollowConfirm"],
                    CssContactsFollowers = config["CssContactsFollowers"],
                    CssContactsFollowing = config["CssContactsFollowing"],
                    CssContactsListScrollable = config["CssContactsListScrollable"],
                    CssCookiesWarning = config["CssCookiesWarning"],
                    CssLoginEmail = config["CssLoginEmail"],
                    CssLoginMyself = config["CssLoginMyself"],
                    CssLoginPassword = config["CssLoginPassword"],
                    CssLoginUnusual = config["CssLoginUnusual"],
                    CssLoginSageInfo = config["CssLoginSageInfo"],
                    CssLoginWarning = config["CssLoginWarning"],
                    CssExplorePhotos = config["CssExplorePhotos"],
                    CssPhotoFollow = config["CssPhotoFollow"],
                    CssPhotoLike = config["CssPhotoLike"],
                    CssSuggestedContact = config["CssSuggestedContact"],
                    SeleniumRemoteServer = config["SeleniumRemoteServer"],
                    UrlContacts = config["UrlContacts"],
                    UrlExplore = config["UrlExplore"],
                    UrlExplorePeopleSuggested = config["UrlExplorePeopleSuggested"],
                    UrlLogin = config["UrlLogin"],
                    UrlRoot = config["UrlRoot"],
                    UrlSearch = config["UrlSearch"]
                };

                try
                {
                    // bool
                    Config.BotCacheMyContacts = int.Parse(config["BotCacheMyContacts"], CultureInfo.InvariantCulture) != 0;
                    Config.BotSaveAfterEachAction = int.Parse(config["BotSaveAfterEachAction"], CultureInfo.InvariantCulture) != 0;
                    Config.BotSaveOnEnd = int.Parse(config["BotSaveOnEnd"], CultureInfo.InvariantCulture) != 0;
                    Config.BotSaveOnLoop = int.Parse(config["BotSaveOnLoop"], CultureInfo.InvariantCulture) != 0;
                    Config.BotUsePersistence = int.Parse(config["BotUsePersistence"], CultureInfo.InvariantCulture) != 0;
                    // float
                    Config.BotSeleniumTimeoutSec = float.Parse(config["BotSeleniumTimeoutSec"], CultureInfo.InvariantCulture);
                    // int
                    Config.BotCacheTimeLimitHours = int.Parse(config["BotCacheTimeLimitHours"], CultureInfo.InvariantCulture);
                    Config.BotFollowTaskBatchMaxLimit = int.Parse(config["BotFollowTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
                    Config.BotFollowTaskBatchMinLimit = int.Parse(config["BotFollowTaskBatchMinLimit"], CultureInfo.InvariantCulture);
                    Config.BotHomeLikeInitScrools = int.Parse(config["BotHomeLikeInitScrools"], CultureInfo.InvariantCulture);
                    Config.BotKeepSomeUnfollowerContacts = int.Parse(config["BotKeepSomeUnfollowerContacts"], CultureInfo.InvariantCulture);
                    Config.BotExplorePhotosScrools = int.Parse(config["BotExplorePhotosScrools"], CultureInfo.InvariantCulture);
                    Config.BotExplorePeopleSuggestedScrools = int.Parse(config["BotExplorePeopleSuggestedScrools"], CultureInfo.InvariantCulture);
                    Config.BotSearchScrools = int.Parse(config["BotSearchScrools"], CultureInfo.InvariantCulture);
                    Config.BotStepFollowMaxWaitMs = int.Parse(config["BotStepFollowMaxWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotStepFollowMinWaitMs = int.Parse(config["BotStepFollowMinWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotStepLikeMaxWaitMs = int.Parse(config["BotStepLikeMaxWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotStepLikeMinWaitMs = int.Parse(config["BotStepLikeMinWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotStepMaxWaitMs = int.Parse(config["BotStepMaxWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotStepMinWaitMs = int.Parse(config["BotStepMinWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotUnfollowTaskBatchMaxLimit = int.Parse(config["BotUnfollowTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
                    Config.BotUnfollowTaskBatchMinLimit = int.Parse(config["BotUnfollowTaskBatchMinLimit"], CultureInfo.InvariantCulture);
                    Config.BotUsePersistenceLimitHours = int.Parse(config["BotUsePersistenceLimitHours"], CultureInfo.InvariantCulture);
                    Config.BotLikeTaskBatchMaxLimit = int.Parse(config["BotLikeTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
                    Config.BotLikeTaskBatchMinLimit = int.Parse(config["BotLikeTaskBatchMinLimit"], CultureInfo.InvariantCulture);
                    Config.BotWaitTaskMaxWaitMs = int.Parse(config["BotWaitTaskMaxWaitMs"], CultureInfo.InvariantCulture);
                    Config.BotWaitTaskMinWaitMs = int.Parse(config["BotWaitTaskMinWaitMs"], CultureInfo.InvariantCulture);
                    Config.SeleniumRemoteServerWarmUpWaitMs = int.Parse(config["SeleniumRemoteServerWarmUpWaitMs"], CultureInfo.InvariantCulture);
                    Config.SeleniumWindowMaxH = int.Parse(config["SeleniumWindowMaxH"], CultureInfo.InvariantCulture);
                    Config.SeleniumWindowMaxW = int.Parse(config["SeleniumWindowMaxW"], CultureInfo.InvariantCulture);
                    Config.SeleniumWindowMinH = int.Parse(config["SeleniumWindowMinH"], CultureInfo.InvariantCulture);
                    Config.SeleniumWindowMinW = int.Parse(config["SeleniumWindowMinW"], CultureInfo.InvariantCulture);
                    Config.BotHomeLikeTaskBatchMaxLimit = int.Parse(config["BotHomeLikeTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
                    Config.BotHomeLikeTaskBatchMinLimit = int.Parse(config["BotHomeLikeTaskBatchMinLimit"], CultureInfo.InvariantCulture);

                    if (int.TryParse(config["BotLoopTaskLimit"], out int tmpBotLoopTaskLimit))
                    {
                        Config.BotLoopTaskLimit = tmpBotLoopTaskLimit;
                    }
                    else
                    {
                        Config.BotLoopTaskLimit = 0;
                    }

                    if (!string.IsNullOrWhiteSpace(config["SeleniumBrowserArguments"]))
                    {
                        Config.SeleniumBrowserArguments = config["SeleniumBrowserArguments"].Split('|', StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        Config.SeleniumBrowserArguments = Enumerable.Empty<string>();
                    }
                }
                catch (FormatException ex)
                {
                    throw new FormatException("Bot settings format error, check your settings", ex);
                }
            }
            else
            {
                throw new FormatException("Configuration file missing : " + configJsonPath);
            }
        }
    }
}