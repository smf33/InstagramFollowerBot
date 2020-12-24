using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFB
{
    internal class SnapshootAction : IDeactivatableAction
    {
        private readonly ILogger<SnapshootAction> _logger;
        private readonly PersistenceManager _persistenceManager;
        private readonly SeleniumWrapper _seleniumWrapper;
        private readonly SnapshootOptions _snapshootOptions;

        public SnapshootAction(ILogger<SnapshootAction> logger, IOptions<SnapshootOptions> snapshootOptions, PersistenceManager persistenceManager, SeleniumWrapper seleniumWrapper) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new SnapshootAction()");
            _snapshootOptions = snapshootOptions.Value ?? throw new ArgumentNullException(nameof(snapshootOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));

            // default
            EnableTask = true;
        }

        public bool EnableTask { get; set; }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            if (_snapshootOptions.MakeSnapShootEachSeconds > 0)
            {
                if (EnableTask)
                {
                    _seleniumWrapper.EnableTimerSnapShoot(_persistenceManager.BaseFileName, _snapshootOptions.MakeSnapShootEachSeconds * 1000);
                }
                else
                {
                    _seleniumWrapper.DisableTimerSnapShoot();
                }
            }

            await Task.CompletedTask; // fake await since no awaitable action here
        }
    }
}