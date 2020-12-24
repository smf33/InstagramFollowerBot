using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFB
{
    internal class DumpingAction
    {
        private readonly DumpingOptions _dumpingOptions;
        private readonly ILogger<DumpingAction> _logger;
        private readonly PersistenceManager _persistenceManager;
        private readonly SeleniumWrapper _seleniumWrapper;

        public DumpingAction(ILogger<DumpingAction> logger, IOptions<DumpingOptions> dumpingOptions, SeleniumWrapper seleniumWrapper, PersistenceManager persistenceManager) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new DumpAction()");
            _dumpingOptions = dumpingOptions?.Value ?? throw new ArgumentNullException(nameof(dumpingOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
            _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
        }

        public void Run()
        {
            _logger.LogTrace("Run()");

            // do the dump requested
            if (_dumpingOptions.DumpBrowserContextOnCrash)
            {
                string fileNameBase = string.Concat(_persistenceManager.BaseFileName, ".", DateTime.Now.ToString("yyyyMMdd-HHmmss")); // no fraction of second, so no conflit with the beginshapshoot task

                _seleniumWrapper.SafeDumpCurrentHtml(string.Concat(fileNameBase, ".html"));

                _seleniumWrapper.SafeDumpCurrentPng(string.Concat(fileNameBase, ".png"));
            }
            else
            {
                _logger.LogDebug("DumpBrowserContextOnCrash is disabled");
            }
        }
    }
}