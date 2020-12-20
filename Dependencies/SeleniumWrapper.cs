using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
    internal class SeleniumWrapper : IBotAction, IDisposable
    {
        private readonly ILogger<SeleniumWrapper> _logger;
        private readonly SeleniumOptions _seleniumOptions;
        private readonly WaitAction _waitAction;

        private TimeSpan NormalWaiter;
        private IWebDriver WebDriver;
        private IJavaScriptExecutor JsDriver;
        private bool disposedValue;

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
        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");
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
                _logger.LogDebug("NewChromeSeleniumWrapper({0}, {1}, {2})", Files.ExecutablePath, w, h);
                WebDriver = new ChromeDriver(Files.ExecutablePath, options);
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

        #region Click

        internal async Task<bool> Click(string cssSelector, bool canBeMissing = false, bool noImplicitWait = true, bool thenWait = true)
        {
            _logger.LogTrace("Click({0})", cssSelector);

            IWebElement found = GetElement(cssSelector
                , noImplicitWait: noImplicitWait
                , canBeMissing: canBeMissing);

            if (found != null)
            {
                Actions action = new Actions(WebDriver);
                action
                    .MoveToElement(found
                        , PseudoRandom.Next(0, found.Size.Width)
                        , PseudoRandom.Next(0, found.Size.Height))
                    .Click()
                    .Build()
                    .Perform();

                if (thenWait)
                {
                    await _waitAction.PostActionsWait();
                }
                return true;
            }
            else // canBeMissing have been managed by GetElement()
            {
                return false;
            }
        }

        #endregion Click

        #region Keyboard

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

        #endregion Keyboard

        #region Scroll

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

        internal async Task ScrollToTopAsync(bool thenWait = true)
        {
            _logger.LogTrace("ScrollToTopAsync()");
            await JsScrollAsync("window.scrollTo(document.body.scrollWidth/2, 0)", thenWait);
        }

        internal async Task ScrollToBottomAsync(int loop = 1, bool thenWait = true)
        {
            _logger.LogTrace("ScrollToBottomAsync({0})", loop);
            for (int i = 0; i < loop; i++)
            {
                await JsScrollAsync("window.scrollTo(document.body.scrollWidth/2, document.body.scrollHeight)", thenWait);
            }
        }

        internal async Task ScrollIntoView(IWebElement element, bool thenWait = true)
        {
            _logger.LogTrace("ScrollIntoView()");
            await JsScrollAsync("arguments[0].parentNode.scrollIntoView(false);", thenWait, element);
        }

        #endregion Scroll

        #region FindElements

        internal IWebElement GetElement(string cssSelector, bool displayedOnly = true, bool canBeMissing = false, bool noImplicitWait = true)
        {
            _logger.LogTrace("GetElement({0})", cssSelector);

            if (noImplicitWait)
            {
                WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            }

            IWebElement found = WebDriver
                .FindElements(By.CssSelector(cssSelector))
                .FirstOrDefault(x => x.Displayed == displayedOnly);

            if (noImplicitWait)
            {
                WebDriver.Manage().Timeouts().ImplicitWait = NormalWaiter;
            }

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
                throw new InvalidOperationException("Element not found : " + cssSelector);
            }
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

        #endregion FindElements

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            _logger.LogTrace("Dispose({0})", disposing);
            if (!disposedValue)
            {
                if (disposing)
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

                // set large fields to null
                JsDriver = null;
                WebDriver = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}