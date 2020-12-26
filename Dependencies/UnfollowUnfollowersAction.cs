using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;

namespace IFB
{
    internal class UnfollowUnfollowersAction : IBotAction
    {
        private readonly InstagramOptions _instagramOptions;
        private readonly ILogger<UnfollowUnfollowersAction> _logger;
        private readonly PersistenceManager _persistenceManager;
        private readonly SeleniumWrapper _seleniumWrapper;
        private readonly UnfollowUnfollowersOptions _unfollowUnfollowersOptions;
        private readonly WaitAction _waitAction;

        public UnfollowUnfollowersAction(ILogger<UnfollowUnfollowersAction> logger, IOptions<UnfollowUnfollowersOptions> unfollowUnfollowersOptions, IOptions<InstagramOptions> instagramOptions, SeleniumWrapper seleniumWrapper, PersistenceManager persistenceManager, WaitAction waitAction) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new UnfollowUnfollowers()");
            _unfollowUnfollowersOptions = unfollowUnfollowersOptions.Value ?? throw new ArgumentNullException(nameof(unfollowUnfollowersOptions));
            _instagramOptions = instagramOptions.Value ?? throw new ArgumentNullException(nameof(instagramOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
            _waitAction = waitAction ?? throw new ArgumentNullException(nameof(waitAction));
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            await _seleniumWrapper.ScrollToTopAsync();

            // open if require
            if (_seleniumWrapper.CurrentUrl != _persistenceManager.Session.UserContactUrl)
            {
                await _seleniumWrapper.Click(_instagramOptions.CssHeaderMyself);
                await _seleniumWrapper.Click(_instagramOptions.CssHeaderMyselfProfile); // must be here, else the auth have failed
            }

            // need to get a new list
            HashSet<string> myFollowers = await GetMyFollowers();

            // open Following popup
            await _seleniumWrapper.Click(_instagramOptions.CssContactsFollowing);

            // loop until no more element
            await _seleniumWrapper.ScrollToBottomAsync(_instagramOptions.CssContactsListScrollable);

            // Get the list
            HashSet<string> myFollowing = new HashSet<string>(_seleniumWrapper.GetAttributes(_instagramOptions.CssContactUrl));
            myFollowing.ExceptWith(myFollowers);
            _logger.LogDebug("Found {0} currently followed but not follower", myFollowing.Count);

            // find
            int unfollowDone = 0;
            int unfollowTodo = PseudoRandom.Next(_unfollowUnfollowersOptions.UnfollowMin, _unfollowUnfollowersOptions.UnfollowMax);
            IWebElement element = GetNextElement(myFollowing);
            while (element != null && unfollowDone < unfollowTodo)
            {
                _logger.LogDebug("Unfollowing");

                // Unfollowing popup
                await _seleniumWrapper.ScrollIntoView(element, thenWait: false); //  bigger waiter altready present after
                await _waitAction.PreFollowWait();
                await _seleniumWrapper.Click(element);

                // Unfollowing action
                await _seleniumWrapper.Click(_instagramOptions.CssContactUnfollowConfirm);

                _seleniumWrapper.CrashIfPresent(_instagramOptions.CssActionWarning, InstagramOptions.CssActionWarningErrorMessage);
                unfollowDone++;

                // prepare next
                element = GetNextElement(myFollowing);
            }
            if (unfollowDone == unfollowTodo)
            {
                _logger.LogDebug("Unfollow actions : {0} unfollowed", unfollowDone);
            }
            else
            {
                _logger.LogDebug("Unfollow actions : {0}/{1} unfollowed (not all done, seem not any more user to unfollow)", unfollowDone, unfollowTodo);
            }

            // close the popup
            await _seleniumWrapper.Click(_instagramOptions.CssContactsClose);
        }

        private async Task<HashSet<string>> GetMyFollowers()
        {
            HashSet<string> myFollowers;
            if (_unfollowUnfollowersOptions.CacheFollowersDetectionHours <= 0
                || !_persistenceManager.Session.MyFollowersUpdate.HasValue
                || _persistenceManager.Session.MyFollowersUpdate.Value.AddHours(_unfollowUnfollowersOptions.CacheFollowersDetectionHours) < DateTime.UtcNow)
            {
                // open Follower popup
                await _seleniumWrapper.Click(_instagramOptions.CssContactsFollowers);

                // loop until no more element
                await _seleniumWrapper.ScrollToBottomAsync(_instagramOptions.CssContactsListScrollable);

                // Get the list
                myFollowers = new HashSet<string>(_seleniumWrapper.GetAttributes(_instagramOptions.CssContactUrl));
                _logger.LogDebug("Found {0} currently followers", myFollowers.Count);

                // close the popup
                await _seleniumWrapper.Click(_instagramOptions.CssContactsClose);

                // save list ?
                if (_unfollowUnfollowersOptions.CacheFollowersDetectionHours > 0)
                {
                    _persistenceManager.Session.MyFollowers = myFollowers;
                    _persistenceManager.Session.MyFollowersUpdate = DateTime.UtcNow;
                }
            }
            else
            {
                myFollowers = _persistenceManager.Session.MyFollowers;
                _logger.LogDebug("Use {0} cached follower", myFollowers.Count);
            }

            return myFollowers;
        }

        private IWebElement GetNextElement(HashSet<string> myFollowingNotFollowers)
        {
            IWebElement element = null;

            while (element == null && myFollowingNotFollowers.Any())
            {
                string fullUrl = myFollowingNotFollowers.First();
                myFollowingNotFollowers.Remove(fullUrl);
                string urlFormated = fullUrl.Substring(_instagramOptions.UrlRoot.Length);
                element = _seleniumWrapper.GetElementByXPath(_instagramOptions.XPathFollowingPopupGetFollowingButtonFromUser, urlFormated, canBeMissing: true);
            }

            return element;
        }
    }
}