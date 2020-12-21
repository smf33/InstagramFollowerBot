using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFB
{
    internal class ActivityAction : IBotAction
    {
        private readonly InstagramOptions _instagramOptions;
        private readonly ILogger<ActivityAction> _logger;
        private readonly SeleniumWrapper _seleniumWrapper;

        public ActivityAction(ILogger<ActivityAction> logger, IOptions<InstagramOptions> instagramOptions, SeleniumWrapper seleniumWrapper) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new ActivityAction()");
            _instagramOptions = instagramOptions.Value ?? throw new ArgumentNullException(nameof(instagramOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            await _seleniumWrapper.ScrollToTopAsync();

            // open
            await _seleniumWrapper.Click(_instagramOptions.CssHeaderButtonActivity);

            // close, a hidden div will catch the click but we usea click by position so it will work for selenium
            await _seleniumWrapper.Click(_instagramOptions.CssHeaderButtonActivity);
        }
    }
}