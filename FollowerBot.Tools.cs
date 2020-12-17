using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace InstagramFollowerBot
{
    public partial class FollowerBot
    {
        private static readonly Random _PseudoRand = new Random(); // this pseudorandom number generator is safe here.

        private static int PseudoRand(int min, int max)
        {
            return _PseudoRand.Next(min, max + 1);
        }

        private void ScrollToTopWait()
        {
            Selenium.ScrollToTop();
            WaitMin();
        }

        private void ScrollToBottomWait()
        {
            Selenium.ScrollToBottom();
            WaitMin();
        }

        private void SchroolDownWaitLoop(int loop)
        {
            for (int i = 0; i < loop; i++)
            {
                ScrollToBottomWait();
            }
        }

        private void SchroolDownWaitLoop(string className)
        {
            string oldValue, newValue = null;
            int it = 0;
            do
            {
                oldValue = newValue;

                // accelerator
                for (int i = 0; i < it; i++)
                {
                    Selenium.ScrollToBottom(className);
                }
                it++;

                newValue = Selenium.ScrollToBottom(className);
                WaitMin();
            }
            while (oldValue != newValue);
        }

        private void WaitMin()
        {
            Task.Delay(Config.BotStepMinWaitMs)
                .Wait();
        }

        private void WaitHumanizer()
        {
            Task.Delay(PseudoRand(Config.BotStepMinWaitMs, Config.BotStepMaxWaitMs))
                    .Wait();
        }

        private void WaitBeforeFollowHumanizer()
        {
            Task.Delay(PseudoRand(Config.BotStepFollowMinWaitMs, Config.BotStepFollowMaxWaitMs))
                    .Wait();
        }

        private void WaitBeforeLikeHumanizer()
        {
            Task.Delay(PseudoRand(Config.BotStepLikeMinWaitMs, Config.BotStepLikeMaxWaitMs))
                    .Wait();
        }

        private void WaitUrlStartsWith(string url)
        {
            while (!Selenium.Url.StartsWith(url, StringComparison.OrdinalIgnoreCase))
            {
                Log.LogDebug("WaitUrlStartsWith...");
                WaitMin();
            }
        }

        private void MoveToWait(string partialOrNotUrl, bool forceReload = false)
        {
            Log.LogDebug("GET {0}", partialOrNotUrl);
            string target;
            if (partialOrNotUrl.StartsWith(Config.UrlRoot, StringComparison.OrdinalIgnoreCase))
            {
                target = partialOrNotUrl;
            }
            else
            {
                target = Config.UrlRoot + partialOrNotUrl;
            }
            if (!target.Equals(Selenium.Url, StringComparison.OrdinalIgnoreCase) || forceReload)
            {
                Selenium.Url = target;
                WaitHumanizer();
            }
        }

        private void ClickWaitIfPresent(string cssSelector)
        {
            if (Selenium.ClickIfPresent(cssSelector))
            {
                WaitHumanizer();
            }
        }

        private void ScrollClickWait(IWebElement element)
        {
            Selenium.ScrollIntoView(element);
            WaitMin();
            element.Click();
            WaitHumanizer();
        }

        private void ClickWait(IWebElement element)
        {
            element.Click();
            WaitHumanizer();
        }

        private void ClickWait(string cssSelector)
        {
            Selenium.Click(cssSelector);
            WaitHumanizer();
        }

        private void ClickByPositionWait(string cssSelector)
        {
            Selenium.ClickByPosition(cssSelector);
            WaitHumanizer();
        }

        private void CheckActionWarning()
        {
            // issue detection : too many actions lately ? should stop for 24-48h...
            Selenium.CrashIfPresent(Config.CssActionWarning, "This action was blocked. Please try again later");
        }

        private static IEnumerable<string> GetTasks(string runTasks, bool botSaveAfterEachAction, bool botSaveOnEnd, bool botSaveOnLoop, int botLoopTaskLimit)
        {
            StringBuilder tasks = new StringBuilder(runTasks.ToUpperInvariant());
            if (botSaveAfterEachAction)
            {
                tasks = tasks
                    .Replace(",", ",SAVE,") // brut
                    .Replace("WAIT,SAVE", "WAIT") // useless save removed
                    .Replace("LOOP,SAVE", "LOOP") // useless save removed
                    .Replace("BEGINLOOP,SAVE", "BEGINLOOP"); // useless save removed
            }
            if (botSaveOnEnd || botSaveAfterEachAction) // botSaveAfterEachAction because last action doesn t have a , after and the replace didn t added it
            {
                tasks = tasks
                    .Append(",SAVE"); // last one
            }
            if (botSaveOnLoop && !botSaveAfterEachAction)
            {
                tasks = tasks
                    .Replace(",LOOP", ",SAVE,LOOP");
            }
            tasks = tasks
                .Replace("SAVE,LOOP,SAVE", "SAVE,LOOP") // small optim if Save on loop and save at end and finish by a loop
                .Replace("SAVE,SAVE", "SAVE"); // both config save management could have duplicated this task

            string computedTasks = tasks.ToString();

            // Loop Management
            int iEnd = computedTasks
                            .IndexOf(",LOOP");
            if (iEnd > 0)
            {
                int iStart = computedTasks.IndexOf("BEGINLOOP,");
                tasks = new StringBuilder(computedTasks); // faster work on string
                tasks = tasks
                    .Replace(",LOOP", "");
                string loopedTasks;
                if (iStart >= 0)
                {
                    iEnd -= 10;
                    loopedTasks = computedTasks.Substring(iStart + 10, iEnd - iStart);
                    tasks = tasks.Remove(iStart, 10); // BEGINLOOP,
                }
                else
                {
                    loopedTasks = computedTasks.Substring(0, iEnd);
                }

                if (botLoopTaskLimit > 0)
                {
                    for (int i = 0; i < botLoopTaskLimit; i++)
                    {
                        tasks = tasks.Insert(iEnd, loopedTasks);
                        tasks = tasks.Insert(iEnd, ',');
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(botLoopTaskLimit), "Config.BotLoopTaskLimit must be greater than 0 when LOOP task is used");
                }
                computedTasks = tasks.ToString(); // resolve
            }

            return string.Concat("LOGGING,SAVE,", computedTasks)
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls
        private readonly SeleniumWrapper Selenium;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Selenium.Dispose();
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