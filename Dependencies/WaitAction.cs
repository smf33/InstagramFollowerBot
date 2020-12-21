using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFB
{
    internal class WaitAction : IBotAction
    {
        private readonly ILogger<WaitAction> _logger;
        private readonly WaitOptions _waitOptions;

        public WaitAction(ILogger<WaitAction> logger, IOptions<WaitOptions> waitOptions) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new WaitAction()");
            _waitOptions = waitOptions?.Value ?? throw new ArgumentNullException(nameof(waitOptions));
        }

        /// <summary>
        /// WAIT task
        /// </summary>
        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");
            await Task.Delay(PseudoRandom.Next(_waitOptions.WaitTaskMinWaitMs, _waitOptions.WaitTaskMaxWaitMs));
        }

        internal async Task PostActionsWait()
        {
            _logger.LogTrace("PostActionsWait()");
            await Task.Delay(PseudoRandom.Next(_waitOptions.PostActionStepMinWaitMs, _waitOptions.PostActionStepMaxWaitMs));
        }

        internal async Task PostScroolWait()
        {
            _logger.LogTrace("PostScroolWait()");
            await Task.Delay(PseudoRandom.Next(_waitOptions.PostScroolStepMinWaitMs, _waitOptions.PostScroolStepMaxWaitMs));
        }

        internal async Task PreFollowWait()
        {
            _logger.LogTrace("PreFollowWait()");
            await Task.Delay(PseudoRandom.Next(_waitOptions.PreFollowMinWaitMs, _waitOptions.PreFollowMaxWaitMs));
        }

        internal async Task PreLikeWait()
        {
            _logger.LogTrace("PreLikeWait()");
            await Task.Delay(PseudoRandom.Next(_waitOptions.PreLikeMinWaitMs, _waitOptions.PreLikeMaxWaitMs));
        }
    }
}