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
			internal string AddPhotosToFav;
			internal string AddContactsToFav;
			internal string AddContactsToFollow;
			internal string BotUserSaveFolder;
			internal bool BotCacheMyContacts;
			internal bool BotSaveAfterEachAction;
			internal bool BotSaveOnEnd;
			internal bool BotSaveOnLoop;
			internal bool BotUsePersistence;
			internal float BotSeleniumTimeoutSec;
			internal int BotCacheTimeLimitHours;
			internal int BotUnfollowTaskBatchMinLimit;
			internal int BotUnfollowTaskBatchMaxLimit;
			internal int BotFollowTaskBatchMinLimit;
			internal int BotFollowTaskBatchMaxLimit;
			internal int BotStepMaxWaitMs;
			internal int BotStepMinWaitMs;
			internal int BotWaitTaskMaxWaitSec;
			internal int BotWaitTaskMinWaitSec;
			internal int BotKeepSomeUnfollowerContacts;
			internal string BotTasks;
			internal string BotUserEmail;
			internal string BotUserPassword;
			internal string BotSearchKeywords;
			internal string CssContactFollow;
			internal string CssContactFollowed;
			internal string CssContactPhotos;
			internal string CssContactUnfollow;
			internal string CssError500;
			internal string CssPhotosError404;
			internal string CssExploreContact;
			internal string CssLoginEmail;
			internal string CssLoginMyself;
			internal string CssLoginPassword;
			internal string CssLoginWarning;
			internal string CssModalWaiterBalls;
			internal string CssPhotoFave;
			internal string CssPhotoFaved;
			internal string CssPhotos;
			internal string CssPhotosFaved;
			internal string CssRecentContactPost;
			internal string CssSearch;
			internal string CssWaiterBalls;
			internal string SeleniumRemoteServer;
			internal int SeleniumRemoteServerWarmUpWaitMs;
			internal int SeleniumWindowMaxH;
			internal int SeleniumWindowMaxW;
			internal int SeleniumWindowMinH;
			internal int SeleniumWindowMinW;
			internal IEnumerable<string> SeleniumBrowserArguments;
			internal int BotLoopTaskLimited;
			internal int BotPeopleSuggestedScrools;
			internal string UrlContacts;
			internal string UrlContactsBlocked;
			internal string UrlContactsMutual;
			internal string UrlContactsNotFriendAndFamily;
			internal string UrlContactsOneWay;
			internal string UrlExplore;
			internal string UrlLogin;
			internal string UrlRecentContactPost;
			internal string UrlRoot;
			internal string UrlExplorePeopleSuggested;
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
					BotUserEmail = config["BotUserEmail"],
					BotUserPassword = config["BotUserPassword"],
					BotTasks = config["BotTasks"],
					BotSearchKeywords = config["BotSearchKeywords"],
					AddPhotosToFav = config["AddPhotosToFav"],
					AddContactsToFav = config["AddContactsToFav"],
					AddContactsToFollow = config["AddContactsToFollow"],
					BotUserSaveFolder = config["BotUserSaveFolder"],
					SeleniumRemoteServer = config["SeleniumRemoteServer"],
					UrlRoot = config["UrlRoot"],
					UrlRecentContactPost = config["UrlRecentContactPost"],
					UrlLogin = config["UrlLogin"],
					UrlExplore = config["UrlExplore"],
					UrlContacts = config["UrlContacts"],
					UrlContactsOneWay = config["UrlContactsOneWay"],
					UrlContactsMutual = config["UrlContactsMutual"],
					UrlContactsBlocked = config["UrlContactsBlocked"],
					UrlContactsNotFriendAndFamily = config["UrlContactsNotFriendAndFamily"],
					UrlExplorePeopleSuggested = config["UrlExplorePeopleSuggested"],
					CssRecentContactPost = config["CssRecentContactPost"],
					CssError500 = config["CssError500"],
					CssPhotosError404 = config["CssPhotosError404"],
					CssLoginEmail = config["CssLoginEmail"],
					CssLoginPassword = config["CssLoginPassword"],
					CssLoginMyself = config["CssLoginMyself"],
					CssLoginWarning = config["CssLoginWarning"],
					CssContactPhotos = config["CssContactPhotos"],
					CssExploreContact = config["CssExploreContact"],
					CssModalWaiterBalls = config["CssModalWaiterBalls"],
					CssWaiterBalls = config["CssWaiterBalls"],
					CssContactFollow = config["CssContactFollow"],
					CssContactFollowed = config["CssContactFollowed"],
					CssContactUnfollow = config["CssContactUnfollow"],
					CssPhotosFaved = config["CssPhotosFaved"],
					CssPhotos = config["CssPhotos"],
					CssPhotoFave = config["CssPhotoFave"],
					CssPhotoFaved = config["CssPhotoFaved"],
					CssSearch = config["CssSearch"]
				};

				try
				{
					// bool
					Config.BotUsePersistence = int.Parse(config["BotUsePersistence"], CultureInfo.InvariantCulture) != 0;
					Config.BotCacheMyContacts = int.Parse(config["BotCacheMyContacts"], CultureInfo.InvariantCulture) != 0;
					Config.BotSaveAfterEachAction = int.Parse(config["BotSaveAfterEachAction"], CultureInfo.InvariantCulture) != 0;
					Config.BotSaveOnLoop = int.Parse(config["BotSaveOnLoop"], CultureInfo.InvariantCulture) != 0;
					Config.BotSaveOnEnd = !Config.BotSaveOnLoop && int.Parse(config["BotSaveOnEnd"], CultureInfo.InvariantCulture) != 0;
					// float
					Config.BotSeleniumTimeoutSec = float.Parse(config["BotSeleniumTimeoutSec"], CultureInfo.InvariantCulture);
					// int
					Config.BotCacheTimeLimitHours = int.Parse(config["BotCacheTimeLimitHours"], CultureInfo.InvariantCulture);
					Config.BotStepMinWaitMs = int.Parse(config["BotStepMinWaitMs"], CultureInfo.InvariantCulture);
					Config.BotStepMaxWaitMs = int.Parse(config["BotStepMaxWaitMs"], CultureInfo.InvariantCulture);
					Config.BotPeopleSuggestedScrools = int.Parse(config["BotPeopleSuggestedScrools"], CultureInfo.InvariantCulture);
					Config.BotWaitTaskMinWaitSec = int.Parse(config["BotWaitTaskMinWaitSec"], CultureInfo.InvariantCulture);
					Config.BotWaitTaskMaxWaitSec = int.Parse(config["BotWaitTaskMaxWaitSec"], CultureInfo.InvariantCulture);
					Config.BotFollowTaskBatchMinLimit = int.Parse(config["BotFollowTaskBatchMinLimit"], CultureInfo.InvariantCulture);
					Config.BotFollowTaskBatchMaxLimit = int.Parse(config["BotFollowTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
					Config.BotUnfollowTaskBatchMinLimit = int.Parse(config["BotUnfollowTaskBatchMinLimit"], CultureInfo.InvariantCulture);
					Config.BotUnfollowTaskBatchMaxLimit = int.Parse(config["BotUnfollowTaskBatchMaxLimit"], CultureInfo.InvariantCulture);
					Config.BotKeepSomeUnfollowerContacts = int.Parse(config["BotKeepSomeUnfollowerContacts"], CultureInfo.InvariantCulture);
					Config.SeleniumWindowMaxH = int.Parse(config["SeleniumWindowMaxH"], CultureInfo.InvariantCulture);
					Config.SeleniumWindowMaxW = int.Parse(config["SeleniumWindowMaxW"], CultureInfo.InvariantCulture);
					Config.SeleniumWindowMinH = int.Parse(config["SeleniumWindowMinH"], CultureInfo.InvariantCulture);
					Config.SeleniumWindowMinW = int.Parse(config["SeleniumWindowMinW"], CultureInfo.InvariantCulture);
					Config.SeleniumRemoteServerWarmUpWaitMs = int.Parse(config["SeleniumRemoteServerWarmUpWaitMs"], CultureInfo.InvariantCulture);

					if (int.TryParse(config["BotLoopTaskLimited"], out int tmpBotLoopTaskLimited))
					{
						Config.BotLoopTaskLimited = tmpBotLoopTaskLimited;
					}
					else
					{
						Config.BotLoopTaskLimited = 0;
					}

					if (!string.IsNullOrWhiteSpace(config["SeleniumBrowserArguments"]))
					{
						Config.SeleniumBrowserArguments = config["SeleniumBrowserArguments"].Split(';', StringSplitOptions.RemoveEmptyEntries);
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
