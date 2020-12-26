using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;

namespace IFB
{
    internal class FollowBackAction : IBotAction
    {
        private readonly FollowBackOptions _followBackOptions;
        private readonly InstagramOptions _instagramOptions;
        private readonly ILogger<FollowBackAction> _logger;
        private readonly SeleniumWrapper _seleniumWrapper;
        private readonly WaitAction _waitAction;

        public FollowBackAction(ILogger<FollowBackAction> logger, IOptions<FollowBackOptions> followBackOptions, IOptions<InstagramOptions> instagramOptions, SeleniumWrapper seleniumWrapper, WaitAction waitAction) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new FollowBackAction()");
            _followBackOptions = followBackOptions.Value ?? throw new ArgumentNullException(nameof(followBackOptions));
            _instagramOptions = instagramOptions.Value ?? throw new ArgumentNullException(nameof(instagramOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _waitAction = waitAction ?? throw new ArgumentNullException(nameof(waitAction));
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            // open profile
            await _seleniumWrapper.Click(_instagramOptions.CssHeaderMyself);
            await _seleniumWrapper.Click(_instagramOptions.CssHeaderMyselfProfile); // must be here, else the auth have failed

            // open followers popup
            await _seleniumWrapper.Click(_instagramOptions.CssContactsFollowers);

            // find
            int followDone = 0;
            int followTodo = PseudoRandom.Next(_followBackOptions.FollowMin, _followBackOptions.FollowMax);
            IWebElement element = _seleniumWrapper.GetElement(_instagramOptions.CssFollowerFollowable);
            while (element != null && followDone < followTodo)
            {
                // like
                _logger.LogDebug("Following");
                await _seleniumWrapper.ScrollIntoView(element, thenWait: false); //  bigger waiter altready present after
                await _waitAction.PreFollowWait();
                await _seleniumWrapper.Click(element);
                _seleniumWrapper.CrashIfPresent(_instagramOptions.CssActionWarning, InstagramOptions.CssActionWarningErrorMessage);
                followDone++;

                // prepare next
                element = _seleniumWrapper.GetElement(_instagramOptions.CssFollowerFollowable);
            }
            if (followDone == followTodo)
            {
                _logger.LogDebug("Follow back actions : {0} follow", followDone);
            }
            else
            {
                _logger.LogDebug("Follow back actions : {0}/{1} follow (not all done, seem not any more user to follow back)", followDone, followTodo);
            }

            // close the popup
            await _seleniumWrapper.Click(_instagramOptions.CssContactsClose);

            throw new NotImplementedException();
        }
    }
}