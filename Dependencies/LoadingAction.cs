using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IFB
{
    internal class LoadingAction : IBotAction
    {
        private readonly ILogger<LoadingAction> _logger;
        private readonly PersistenceManager _persistenceManager;
        private readonly SeleniumWrapper _seleniumWrapper;

        public LoadingAction(ILogger<LoadingAction> logger, SeleniumWrapper seleniumWrapper, PersistenceManager persistenceManager) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new LoadingAction()");
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");

            await _persistenceManager.LoadPersistence();

            await _seleniumWrapper.LoadSelenium();
        }
    }
}