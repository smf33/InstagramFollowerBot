using System;
using System.Collections.Generic;
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
		private const string DetectPeopleSuggestedStr = "DETECTPEOPLESUGGESTED";
		private const string DoContactsFollowStr = "DOCONTACTSFOLLOW";
		private const string DoContactsUnfollowStr = "DOCONTACTSUNFOLLOW";
		private const string PauseStr = "PAUSE";
		private const string WaitStr = "WAIT";
		private const string LoopStr = "LOOP";
		private const string LoopStartStr = "LOOPSTART";
		private const string SaveStr = "SAVE";

		private static readonly string ExecPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		private readonly ILogger Log;

		public FollowerBot(string[] configArgs, ILogger logger)
		{
			Log = logger;

			Log.LogInformation("## LOADING...");
			LoadConfig(configArgs);

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

		public void Run()
		{
			Log.LogInformation("## LOGGING...");

			LoadCookies();
			AuthLogin();

			Log.LogInformation("Logged user :  {0}", Data.UserContactUrl);
			PostAuthInit();
			SaveData(); // save cookies at last

			Log.LogInformation("## RUNNING...");
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
				switch (curTask)
				{
					case DetectPeopleSuggestedStr:
						DetectPeopleSuggested();
						break;
					case DetectContactsFollowBackStr:
						DetectContactsFollowBack();
						break;
					case DoContactsFollowStr:
						DoContactsFollow();
						break;
					case DetectContactsUnfollowBackStr:
						DetectContactsUnfollowBack();
						break;
					case DoContactsUnfollowStr:
						DoContactsUnfollow();
						break;
					case SaveStr: // managed in the if after
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
