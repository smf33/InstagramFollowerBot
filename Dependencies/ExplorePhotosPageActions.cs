using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;

namespace IFB
{
    internal class ExplorePhotosPageActions : ILikeAction, IFollowAction
    {
        private readonly ILogger<HomePageAction> _logger;
        private readonly ExplorePhotosPageActionsOptions _explorePhotosPageActionsOptions;
        private readonly InstagramOptions _instagramOptions;
        private readonly SeleniumWrapper _seleniumWrapper;
        private readonly WaitAction _waitAction;

        public bool DoFollow { get; set; }
        public bool DoLike { get; set; }

        public ExplorePhotosPageActions(ILogger<HomePageAction> logger, IOptions<ExplorePhotosPageActionsOptions> explorePhotosPageActionsOptions, IOptions<InstagramOptions> instagramOptions, SeleniumWrapper seleniumWrapper, WaitAction waitAction) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new ExplorePhotosPageActions()");
            _explorePhotosPageActionsOptions = explorePhotosPageActionsOptions.Value ?? throw new ArgumentNullException(nameof(explorePhotosPageActionsOptions));
            _instagramOptions = instagramOptions.Value ?? throw new ArgumentNullException(nameof(instagramOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _waitAction = waitAction ?? throw new ArgumentNullException(nameof(waitAction));
            DoFollow = true;
            DoLike = true;
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            await _seleniumWrapper.ScrollToTopAsync();

            // open
            await _seleniumWrapper.Click(_instagramOptions.CssHeaderButtonExplore);

            // scrools
            await _seleniumWrapper.ScrollToBottomAsync(_explorePhotosPageActionsOptions.InitScrools);

            // find
            int followDone = 0;
            int likeDone = 0;
            int followTodo = DoFollow ? PseudoRandom.Next(_explorePhotosPageActionsOptions.FollowMin, _explorePhotosPageActionsOptions.FollowMax) : 0;
            int likeTodo = DoLike ? PseudoRandom.Next(_explorePhotosPageActionsOptions.LikeMin, _explorePhotosPageActionsOptions.LikeMax) : 0;
            IWebElement element = _seleniumWrapper.GetElement(_instagramOptions.CssExplorePhotos);
            while (element != null && (likeDone < likeTodo || followDone < followTodo))
            {
                _logger.LogDebug("Opening a post");
                await _seleniumWrapper.ScrollIntoView(element);
                element.Click();
                await _waitAction.PostScroolWait(); // give small time to the popup to open

                // Follow
                if (followDone < followTodo && _seleniumWrapper.GetElementIfPresent(_instagramOptions.CssPhotoFollow, out IWebElement btnFollow))
                {
                    _logger.LogDebug("Following");
                    await _waitAction.PreFollowWait();
                    btnFollow.Click();
                    await _waitAction.PostActionsWait();
                    _seleniumWrapper.CrashIfPresent(_instagramOptions.CssActionWarning, InstagramOptions.CssActionWarningErrorMessage);
                    followDone++;
                }

                // like
                if (likeDone < likeTodo && _seleniumWrapper.GetElementIfPresent(_instagramOptions.CssPhotoLike, out IWebElement btnLike))
                {
                    _logger.LogDebug("Liking");
                    await _waitAction.PreLikeWait();
                    btnLike.Click();
                    await _waitAction.PostActionsWait();
                    _seleniumWrapper.CrashIfPresent(_instagramOptions.CssActionWarning, InstagramOptions.CssActionWarningErrorMessage);
                    likeDone++;
                }

                // close modal page without waiter
                await _seleniumWrapper.Click(_instagramOptions.CssPhotoClose);

                // prepare next
                await _seleniumWrapper.ScrollToBottomAsync();
                element = _seleniumWrapper.GetElement(_instagramOptions.CssExplorePhotos);
            }
            if (likeDone == likeTodo)
            {
                _logger.LogDebug("Explore Photos page actions : {0} like, {1} follow", likeDone, followDone);
            }
            else
            {
                _logger.LogWarning("Explore Photos page actions : {0}/{1} like, {2}/{3} follow (not all done, you may increase scrool or reduce frequency)", likeDone, likeTodo, followDone, followTodo);
            }
        }
    }
}