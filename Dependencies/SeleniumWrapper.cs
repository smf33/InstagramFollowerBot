using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;

namespace IFB
{
    internal class SeleniumWrapper : IDisposable
    {
        private readonly ILogger<SeleniumWrapper> _logger;
        private readonly SeleniumOptions _seleniumOptions;
        private readonly WaitAction _waitAction;

        private bool disposedValue;
        private IJavaScriptExecutor JsDriver;
        private TimeSpan NormalWaiter;
        private IWebDriver WebDriver;

        public SeleniumWrapper(ILogger<SeleniumWrapper> logger, IOptions<SeleniumOptions> seleniumOptions, WaitAction waitAction) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new SeleniumWrapper()");
            _seleniumOptions = seleniumOptions?.Value ?? throw new ArgumentNullException(nameof(seleniumOptions));
            _waitAction = waitAction ?? throw new ArgumentNullException(nameof(waitAction));
        }

        #region Loading

        /// <summary>
        /// Load selenium action
        /// </summary>
        internal async Task LoadSelenium()
        {
            _logger.LogTrace("LoadSelenium()");
            // Set Options
            ChromeOptions options = new ChromeOptions
            {
                PageLoadStrategy = PageLoadStrategy.Normal
            };
            string w = PseudoRandom.Next(_seleniumOptions.WindowMinW, _seleniumOptions.WindowMaxW)
                .ToString(CultureInfo.InvariantCulture);
            string h = PseudoRandom.Next(_seleniumOptions.WindowMinH, _seleniumOptions.WindowMaxH)
                .ToString(CultureInfo.InvariantCulture);
            options.AddArgument("--window-size=" + w + "," + h); // try to randomize the setup in order to reduce detection
            foreach (string a in _seleniumOptions.BrowserArguments
                    .Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                options.AddArgument(a);
            }

            // Init Local or remote SeleniumDrivers
            if (string.IsNullOrWhiteSpace(_seleniumOptions.RemoteServer))
            {
                _logger.LogDebug("NewChromeSeleniumWrapper({0}, {1}, {2})", Program.ExecutablePath, w, h);
                WebDriver = new ChromeDriver(Program.ExecutablePath, options);
            }
            else
            {
                await Task.Delay(_seleniumOptions.RemoteServerWarmUpWaitMs);
                if (!Uri.TryCreate(_seleniumOptions.RemoteServer, UriKind.Absolute, out Uri uri)) // may be a hostname ?
                {
                    uri = new Uri("http://" + _seleniumOptions.RemoteServer + ":4444/wd/hub");
                }
                WebDriver = new RemoteWebDriver(uri, options);
            }

            // last setup
            NormalWaiter = TimeSpan.FromSeconds(_seleniumOptions.TimeoutSec);
            WebDriver.Manage().Timeouts().PageLoad = NormalWaiter;
            WebDriver.Manage().Timeouts().ImplicitWait = NormalWaiter;
            WebDriver.Manage().Timeouts().AsynchronousJavaScript = NormalWaiter;

            // init usefull drivers
            JsDriver = (IJavaScriptExecutor)WebDriver;
        }

        #endregion Loading

        #region Session Management

        internal IEnumerable<object> Cookies
        {
            get => WebDriver.Manage().Cookies.AllCookies;
            set
            {
                WebDriver.Manage().Cookies.DeleteAllCookies();
                foreach (JObject cookie in value.OfType<JObject>())
                {
                    Cookie c = Cookie.FromDictionary(cookie.ToObject<Dictionary<string, object>>());
                    WebDriver.Manage().Cookies.AddCookie(c);
                }
            }
        }

        internal IDictionary<string, string> LocalStorage
        {
            get => GetJsStorage("localStorage");
            set => SetJsStorage("localStorage", value);
        }

        internal IDictionary<string, string> SessionStorage
        {
            get => GetJsStorage("sessionStorage");
            set => SetJsStorage("sessionStorage", value);
        }

        private IDictionary<string, string> GetJsStorage(string jsObject)
        {
            IDictionary<string, object> ret = JsDriver.ExecuteScript(string.Concat("return ", jsObject, ";")) as IDictionary<string, object>;
            return new Dictionary<string, string>(ret
                .Where(x => x.Value is string)
                .Select(x => new KeyValuePair<string, string>(x.Key, x.Value as string)));
        }

        private void SetJsStorage(string jsObject, IDictionary<string, string> value)
        {
            StringBuilder s = new StringBuilder(jsObject);
            s.Append(".clear();");
            foreach (KeyValuePair<string, string> kv in value)
            {
                s.AppendFormat("{0}.setItem('{1}', '{2}');", jsObject, kv.Key, kv.Value);
            }
            JsDriver.ExecuteScript(s.ToString());
        }

        #endregion Session Management

        internal string CurrentUrl => WebDriver.Url;

        internal async Task MoveToAsync(string absoluteUrl, bool forceReload = false, bool thenWait = true)
        {
            _logger.LogTrace("MoveToAsync({0})", absoluteUrl);

            if (WebDriver.Url != absoluteUrl || forceReload)
            {
                WebDriver.Url = absoluteUrl;
            }

            if (thenWait)
            {
                await _waitAction.PostActionsWait();
            }
        }

        internal async Task WaitLoader(string cssSelector)
        {
            _logger.LogTrace("WaitLoader({0})", cssSelector);

            IWebElement element = GetElement(cssSelector, canBeMissing: true);
            if (element != null)
            {
                _logger.LogDebug("Wait until loading end");
                DateTime timeout = DateTime.UtcNow.Add(NormalWaiter); // timeout
                do
                {
                    await _waitAction.PostScroolWait(); // small waiter
                    element = GetElement(cssSelector, canBeMissing: true);
                }
                while (element != null && DateTime.UtcNow < timeout);
                // has hit timeout ?
                if (element != null)
                {
                    throw new TimeoutException("Loading take too much time...");
                }
            }
        }

        #region Click

        internal async Task<bool> Click(string cssSelector, bool canBeMissing = false, bool noImplicitWait = true, bool thenWait = true)
        {
            _logger.LogTrace("Click({0})", cssSelector);

            // find the element and crash if necessarie
            IWebElement element = GetElement(cssSelector, noImplicitWait: noImplicitWait, canBeMissing: canBeMissing);

            // exception have been raise if canBeMissing== false and not found
            if (element != null)
            {
                await Click(element, thenWait: thenWait);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal async Task Click(IWebElement element, bool thenWait = true)
        {
            _logger.LogTrace("Click({0})", element);
            if (element != null)
            {
                Actions action = new Actions(WebDriver);
                action
                    .MoveToElement(element,
                        GetRandomCenterOffset(element.Size.Width),
                        GetRandomCenterOffset(element.Size.Height))
                    .Click()
                    .Build()
                    .Perform();

                if (thenWait)
                {
                    await _waitAction.PostActionsWait();
                }
            }
            else // here the element have to be present if this is called
            {
                throw new ArgumentNullException(nameof(element));
            }
        }

        // click between the 25% and 75% of the space of the object
        private static int GetRandomCenterOffset(int elementLenght)
        {
            return (elementLenght / 4)  // add 25 %
                + PseudoRandom.Next(0, (elementLenght / 2)); // random in 50% next
        }

        #endregion Click

        #region Keyboard

        internal async Task EnterKeyAsync(string cssSelector, bool thenWait = true)
        {
            _logger.LogTrace("EnterKeyAsync({0})", cssSelector);

            WebDriver
                .FindElement(By.CssSelector(cssSelector))
                .SendKeys(Keys.Enter);

            if (thenWait)
            {
                await _waitAction.PostActionsWait();
            }
        }

        internal async Task InputWriteAsync(string cssSelector, string text, bool thenWait = true)
        {
            _logger.LogTrace("InputWriteAsync({0})", cssSelector);

            WebDriver
                .FindElement(By.CssSelector(cssSelector))
                .SendKeys(text);

            if (thenWait)
            {
                await _waitAction.PostScroolWait();
            }
        }

        #endregion Keyboard

        #region Scroll

        internal async Task ScrollIntoView(IWebElement element, bool thenWait = true)
        {
            _logger.LogTrace("ScrollIntoView({0})", element);
            await JsScrollAsync("arguments[0].parentNode.scrollIntoView(false);", thenWait, element);
        }

        internal async Task ScrollToBottomAsync(int loop = 1, bool thenWait = true)
        {
            _logger.LogTrace("ScrollToBottomAsync({0})", loop);
            for (int i = 0; i < loop; i++)
            {
                await JsScrollAsync("window.scrollTo(document.body.scrollWidth/2, document.body.scrollHeight)", thenWait);
            }
        }

        internal async Task ScrollToBottomAsync(string divNameLooped)
        {
            _logger.LogTrace("ScrollToBottomAsync({0})", divNameLooped);
            string oldScrollTop, newScrollTop = null;
            do
            {
                oldScrollTop = newScrollTop;

                await JsScrollAsync("let containerEl=document.getElementsByClassName('" + divNameLooped + "')[0];containerEl.scrollTop=containerEl.scrollTop==0?200:(containerEl.scrollHeight-containerEl.offsetHeight>containerEl.scrollTop+200?containerEl.scrollTop+200:containerEl.scrollHeight-containerEl.offsetHeight);", true);

                newScrollTop = JsDriver.ExecuteScript("let containerEl=document.getElementsByClassName('" + divNameLooped + "')[0];;return containerEl.scrollTop;").ToString();
            }
            while (oldScrollTop != newScrollTop);
        }

        internal async Task ScrollToTopAsync(bool thenWait = true)
        {
            _logger.LogTrace("ScrollToTopAsync()");
            await JsScrollAsync("window.scrollTo(document.body.scrollWidth/2, 0)", thenWait);
        }

        private async Task JsScrollAsync(string js, bool thenWait, IWebElement element = null)
        {
            if (element == null)
            {
                JsDriver.ExecuteScript(js);
            }
            else
            {
                JsDriver.ExecuteScript(js, element);
            }

            if (thenWait)
            {
                await _waitAction.PostScroolWait();
            }
        }

        #endregion Scroll

        #region FindElements

        internal void CrashIfPresent(string cssSelector, string crashMessage, bool noImplicitWait = true)
        {
            _logger.LogTrace("CrashIfPresent({0})", cssSelector);

            IWebElement found = GetElement(cssSelector
                , noImplicitWait: noImplicitWait
                , canBeMissing: true);

            if (found != null)
            {
                throw new InvalidOperationException(crashMessage);
            }
        }

        internal IEnumerable<string> GetAttributes(string cssSelector, string attribute = "href", bool noImplicitWait = true)
        {
            return GetElements(By.CssSelector(cssSelector), noImplicitWait: noImplicitWait)
                .Select(x => x.GetAttribute(attribute));
        }

        internal IWebElement GetElement(string cssSelector, bool displayedOnly = true, bool canBeMissing = false, bool noImplicitWait = true)
        {
            _logger.LogTrace("GetElement({0})", cssSelector);
            return GetElement(By.CssSelector(cssSelector),
                displayedOnly: displayedOnly, canBeMissing: canBeMissing, noImplicitWait: noImplicitWait);
        }

        internal IWebElement GetElementByXPath(string xPathFormat, string arg0, bool displayedOnly = true, bool canBeMissing = false, bool noImplicitWait = true)
        {
            _logger.LogTrace("GetElementByXPath({0})", arg0);

            return GetElement(By.XPath(string.Format(xPathFormat, arg0)),
                displayedOnly: displayedOnly, canBeMissing: canBeMissing, noImplicitWait: noImplicitWait);
        }

        internal bool GetElementIfPresent(string cssSelector, out IWebElement element, bool noImplicitWait = true)
        {
            _logger.LogTrace("GetElementIfPresent({0})", cssSelector);

            element = GetElement(cssSelector, canBeMissing: true, noImplicitWait: noImplicitWait);

            if (element != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private IWebElement GetElement(By by, bool displayedOnly = true, bool canBeMissing = false, bool noImplicitWait = true)
        {
            IWebElement found = GetElements(by, noImplicitWait: noImplicitWait)
                .FirstOrDefault(x => x.Displayed == displayedOnly);

            if (found != null)
            {
                return found;
            }
            else if (canBeMissing)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException("Element not found : " + by.ToString());
            }
        }

        private IReadOnlyCollection<IWebElement> GetElements(By by, bool noImplicitWait)
        {
            if (noImplicitWait)
            {
                WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            }

            IReadOnlyCollection<IWebElement> found = WebDriver
                .FindElements(by);

            if (noImplicitWait)
            {
                WebDriver.Manage().Timeouts().ImplicitWait = NormalWaiter;
            }

            return found;
        }

        #endregion FindElements

        #region SnapShoot

        private Timer snapShotTimer;
        private string timerSnapShootFileNameBase;

        internal void DisableTimerSnapShoot(bool canBeInvalid = false)
        {
            _logger.LogTrace("DisableTimerSnapShoot()");
            if (snapShotTimer != null)
            {
                snapShotTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else if (!canBeInvalid) // called from crashdump
            {
                throw new InvalidOperationException("Snapshoot haven't been enabled !");
            }
        }

        internal void EnableTimerSnapShoot(string filenamePath, int timerMs)
        {
            _logger.LogTrace("EnableTimerSnapShoot({0})", filenamePath);
            timerSnapShootFileNameBase = string.Concat(filenamePath, ".");
            snapShotTimer = new Timer(TimerSnapShoot, null, 0, timerMs);
        }

        internal void SafeDumpCurrentHtml(string fileName)
        {
            _logger.LogTrace("SafeDumpCurrentHtml({0})", fileName);
            try
            {
                _logger.LogInformation("Dumping current page {0} as {1}.html", WebDriver.Url, fileName);
                string html = JsDriver.ExecuteScript("return document.documentElement.innerHTML").ToString()
                    .Replace("href=\"/", "href=\"https://www.instagram.com/");
                File.WriteAllText(fileName, html);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Couldn't save html page context {0}", ex.GetBaseException().Message);
            }
        }

        internal void SafeDumpCurrentPng(string fileName)
        {
            _logger.LogTrace("SafeDumpCurrentPng({0})", fileName);
            try
            {
                Screenshot ss = ((ITakesScreenshot)WebDriver).GetScreenshot();
                ss.SaveAsFile(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Couldn't save Screenshot of the page {0}", ex.GetBaseException().Message);
            }
        }

        private void TimerSnapShoot(object stateInfo)
        {
            string fileName = string.Concat(timerSnapShootFileNameBase, DateTime.Now.ToString("yyyyMMdd-HHmmss.ff"), ".png");
            SafeDumpCurrentPng(fileName);
        }

        #endregion SnapShoot

        #region IDisposable Support

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _logger.LogTrace("Dispose({0})", disposing);
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (snapShotTimer != null)
                    {
                        snapShotTimer.Dispose();
                    }
                    if (WebDriver != null)
                    {
                        try
                        {
                            WebDriver.Quit();
                        }
                        catch
                        {
                            // disposing
                        }
                        WebDriver.Dispose();
                    }
                }

                // set large fields to null
                snapShotTimer = null;
                JsDriver = null;
                WebDriver = null;

                // report work done
                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}