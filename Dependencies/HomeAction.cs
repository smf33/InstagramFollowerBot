using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;

namespace IFB
{
    internal class HomeAction : ILikeAction
    {
        private readonly ILogger<HomeAction> _logger;
        private readonly HomePageOptions _homePageActionsOptions;
        private readonly InstagramOptions _instagramOptions;
        private readonly SeleniumWrapper _seleniumWrapper;
        private readonly WaitAction _waitAction;

        public bool DoLike { get; set; }

        public HomeAction(ILogger<HomeAction> logger, IOptions<HomePageOptions> homePageActionsOptions, IOptions<InstagramOptions> instagramOptions, SeleniumWrapper seleniumWrapper, WaitAction waitAction) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new HomeAction()");
            _homePageActionsOptions = homePageActionsOptions.Value ?? throw new ArgumentNullException(nameof(homePageActionsOptions));
            _instagramOptions = instagramOptions.Value ?? throw new ArgumentNullException(nameof(instagramOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _waitAction = waitAction ?? throw new ArgumentNullException(nameof(waitAction));
            DoLike = true;
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            await _seleniumWrapper.ScrollToTopAsync();

            // open
            await _seleniumWrapper.Click(_instagramOptions.CssHeaderButtonHome);

            // scrools
            await _seleniumWrapper.ScrollToBottomAsync(_homePageActionsOptions.InitScrools);

            // find
            int likeDone = 0;
            int likeTodo = DoLike ? PseudoRandom.Next(_homePageActionsOptions.LikeMin, _homePageActionsOptions.LikeMax) : 0;
            IWebElement element = _seleniumWrapper.GetElement(_instagramOptions.CssPhotoLike);
            while (element != null && likeDone < likeTodo)
            {
                // like
                _logger.LogDebug("Liking");
                await _waitAction.PreLikeWait();
                await _seleniumWrapper.ScrollIntoView(element);
                _seleniumWrapper.CrashIfPresent(_instagramOptions.CssActionWarning, InstagramOptions.CssActionWarningErrorMessage);
                likeDone++;
                // prepare next
                await _seleniumWrapper.ScrollToBottomAsync();
                element = _seleniumWrapper.GetElement(_instagramOptions.CssPhotoLike);
            }
            if (likeDone == likeTodo)
            {
                _logger.LogDebug("Home page actions : {0} like", likeDone);
            }
            else
            {
                _logger.LogWarning("Home page actions : {0}/{1} liked (not all done, you may increase scrool or reduce frequency)", likeDone, likeTodo);
            }
        }
    }
}