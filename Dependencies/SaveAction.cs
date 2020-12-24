using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFB
{
    internal class SaveAction : IBotAction
    {
        private readonly ILogger<SaveAction> _logger;
        private readonly PersistenceManager _persistenceManager;
        private readonly PersistenceOptions _persistenceOptions;
        private readonly SeleniumWrapper _seleniumWrapper;

        public SaveAction(ILogger<SaveAction> logger, IOptions<PersistenceOptions> persistenceOptions, SeleniumWrapper seleniumWrapper, PersistenceManager persistenceManager) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new SaveAction()");
            _persistenceOptions = persistenceOptions?.Value ?? throw new ArgumentNullException(nameof(persistenceOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
        }

        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");
            if (_persistenceOptions.UsePersistence)
            {
                // get updated selenium session data
                if (!_persistenceManager.Session.CookiesInitDate.HasValue)
                {
                    _persistenceManager.Session.CookiesInitDate = DateTime.UtcNow;
                }
                _persistenceManager.Session.Cookies = _seleniumWrapper.Cookies;
                _persistenceManager.Session.LocalStorage = _seleniumWrapper.LocalStorage;
                _persistenceManager.Session.SessionStorage = _seleniumWrapper.SessionStorage;

                await _persistenceManager.SaveSessionFile();

                _logger.LogDebug("User session saved");
            }
        }
    }
}