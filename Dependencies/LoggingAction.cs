using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFB
{
    internal class LoggingAction : IBotAction
    {
        private readonly ILogger<LoggingAction> _logger;
        private readonly LoggingOptions _loggingOptions;
        private readonly InstagramOptions _instagramOptions;
        private readonly SeleniumWrapper _seleniumWrapper;
        private readonly PersistenceAction _persistenceAction;

        public LoggingAction(ILogger<LoggingAction> logger, IOptions<LoggingOptions> loggingOptions, IOptions<InstagramOptions> instagramOptions, SeleniumWrapper seleniumWrapper, PersistenceAction persistenceAction) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new LoggingAction()");
            _loggingOptions = loggingOptions?.Value ?? throw new ArgumentNullException(nameof(loggingOptions));
            _instagramOptions = instagramOptions.Value ?? throw new ArgumentNullException(nameof(instagramOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _persistenceAction = persistenceAction ?? throw new ArgumentNullException(nameof(persistenceAction));

            // config check
            if (string.IsNullOrWhiteSpace(_loggingOptions.User))
            {
                throw new ArgumentNullException(nameof(loggingOptions), "User is empty !");
            }
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            PersistenceData data = await _persistenceAction.GetSessionAsync(_loggingOptions.User);

            // load page (pre requis for setting cookie)
            await _seleniumWrapper.MoveToAsync(_instagramOptions.UrlRoot);

            // Ignore the message bar : Allow Instagram Cookies
            await _seleniumWrapper.Click(_instagramOptions.CssCookiesWarning, canBeMissing: true);

            if (!await TryAuthCookiesAsync(data))
            {
                await AuthLoginAsync();
                // update the user URL
                data = _persistenceAction.SetNewSession(_seleniumWrapper.CurrentUrl);
            }
            _logger.LogDebug("Logged user :  {0}", data.UserContactUrl);
        }

        private async Task<bool> TryAuthCookiesAsync(PersistenceData data)
        {
            _logger.LogTrace("TryAuthCookies()");
            if (data?.UserContactUrl != null)
            {
                _persistenceAction.UpdateSeleniumFromSession();

                // reload page (using cookie this time)
                await _seleniumWrapper.MoveToAsync(_instagramOptions.UrlRoot, true);

                // Ignore the enable notification on your browser modal popup
                await _seleniumWrapper.Click(_instagramOptions.CssLoginWarning, canBeMissing: true);

                ////check cookie auth OK :  who am i ?
                if (await _seleniumWrapper.Click(_instagramOptions.CssLoginMyself, canBeMissing: true, noImplicitWait: false))
                {
                    string curUserContactUrl = _seleniumWrapper.CurrentUrl;
                    if (data.UserContactUrl.Equals(curUserContactUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("User authentified from cookie");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Cookie authentification not matching : expecting {0} but getting {1}", data.UserContactUrl, curUserContactUrl);
                        return false;
                    }
                }
                else // not present = not identified
                {
                    _logger.LogWarning("Cookie authentification failed");
                    return false;
                }
            }
            else
            {
                _logger.LogDebug("Cookie authentification not used");
                return false;
            }
        }

        private async Task AuthLoginAsync()
        {
            _logger.LogTrace("AuthLogin()");
            await _seleniumWrapper.MoveToAsync(_instagramOptions.UrlRoot);

            await _seleniumWrapper.InputWriteAsync(_instagramOptions.CssLoginEmail, _loggingOptions.User);

            if (!string.IsNullOrWhiteSpace(_loggingOptions.Password))
            {
                await _seleniumWrapper.InputWriteAsync(_instagramOptions.CssLoginPassword, _loggingOptions.Password);
                await _seleniumWrapper.EnterKeyAsync(_instagramOptions.CssLoginPassword);
            }
            else
            {
                _logger.LogInformation("Waiting user manual password validation...");
                _logger.LogWarning("PRESS <ENTER> WHEN LOGGED");
                Console.ReadLine();
            }

            // Humain user need to login with email code check ? In this case, remove password from the config and increase a lot step time (~1min) in order to allow you to pass throu this check process
            _seleniumWrapper.CrashIfPresent(_instagramOptions.CssLoginUnusual, InstagramOptions.CssLoginUnusualErrorMessage);

            // Confirm save user info
            await _seleniumWrapper.Click(_instagramOptions.CssLoginSageInfo, canBeMissing: true);

            // Ignore the enable notification on your browser modal popup
            await _seleniumWrapper.Click(_instagramOptions.CssLoginWarning, canBeMissing: true);

            // who am i ?
            await _seleniumWrapper.Click(_instagramOptions.CssLoginMyself); // must be here, else the auth have failed
        }
    }
}