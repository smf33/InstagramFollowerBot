using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFB
{
    internal class SnapshootAction : IDeactivatableAction
    {
        private readonly ILogger<SnapshootAction> _logger;
        private readonly LoggingOptions _loggingOptions;
        private readonly PersistenceAction _persistenceAction;
        private readonly SeleniumWrapper _seleniumWrapper;
        private readonly SnapshootOptions _snapshootOptions;

        public SnapshootAction(ILogger<SnapshootAction> logger, IOptions<SnapshootOptions> snapshootOptions, IOptions<LoggingOptions> loggingOptions, PersistenceAction persistenceAction, SeleniumWrapper seleniumWrapper) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new SnapshootAction()");
            _snapshootOptions = snapshootOptions.Value ?? throw new ArgumentNullException(nameof(snapshootOptions));
            _loggingOptions = loggingOptions.Value ?? throw new ArgumentNullException(nameof(loggingOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _persistenceAction = persistenceAction ?? throw new ArgumentNullException(nameof(persistenceAction));

            // default
            EnableTask = true;
        }

        public bool EnableTask { get; set; }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            if (EnableTask)
            {
                if (_snapshootOptions.MakeSnapShootEachSeconds > 0)
                {
                    string baseFileName = _persistenceAction.GetSessionBaseFileName(_loggingOptions.User);
                    _seleniumWrapper.EnableTimerSnapShoot(baseFileName, _snapshootOptions.MakeSnapShootEachSeconds * 1000);
                }
            }
            else
            {
                _seleniumWrapper.DisableTimerSnapShoot();
            }

            await Task.CompletedTask; // fake await since no awaitable action here
        }
    }
}