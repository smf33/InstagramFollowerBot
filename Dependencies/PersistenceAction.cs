using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace IFB
{
    internal class PersistenceAction : IBotAction
    {
        private readonly ILogger<PersistenceAction> _logger;
        private readonly PersistenceOptions _persistenceOptions;
        private readonly SeleniumWrapper _seleniumWrapper;

        public PersistenceAction(ILogger<PersistenceAction> logger, IOptions<PersistenceOptions> persistenceOptions, SeleniumWrapper seleniumWrapper) // DI : constructor must be public

        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new PersistenceAction()");
            _persistenceOptions = persistenceOptions?.Value ?? throw new ArgumentNullException(nameof(persistenceOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
        }

        private PersistenceData _Session = null;

        private async Task LoadSessionFile()
        {
            // read raw file
            string content;
            try
            {
                content = await File.ReadAllTextAsync(PersistenceOptions.CurrentLogFile, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Fail to read persistence session data {0} :", PersistenceOptions.CurrentLogFile, ex.GetBaseException().Message);
                throw;
            }

            // deserialize file
            PersistenceData dataTmp;
            try
            {
                dataTmp = JsonConvert.DeserializeObject<PersistenceData>(content);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Fail to deserialize persistence session data {0} : ", PersistenceOptions.CurrentLogFile, ex.GetBaseException().Message);
                throw;
            }

            // check file
            if (_persistenceOptions.UsePersistenceLimitHours <= 0
                || DateTime.UtcNow < dataTmp.CookiesInitDate.Value.AddHours(_persistenceOptions.UsePersistenceLimitHours))
            {
                _logger.LogDebug("User session loaded.");
                _Session = dataTmp;
            }
            else
            {
                _logger.LogWarning("Persistence limit reached, starting a new session");
            }
        }

        private async Task SaveSessionFile()
        {
            // serialize file
            string content;
            try
            {
                content = JsonConvert.SerializeObject(_Session, Formatting.Indented);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Fail to serialize persistence session data : ", ex.GetBaseException().Message);
                throw;
            }

            // write raw file
            try
            {
                await File.WriteAllTextAsync(PersistenceOptions.CurrentLogFile, content, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Fail to write persistence session data {0} :", PersistenceOptions.CurrentLogFile, ex.GetBaseException().Message);
                throw;
            }
        }

        internal async Task<PersistenceData> GetSessionAsync(string userName)
        {
            _logger.LogTrace("GetSessionAsync()");
            if (_persistenceOptions.UsePersistence)
            {
                // get base path
                if (!string.IsNullOrWhiteSpace(_persistenceOptions.SaveFolder))
                {
                    if (!Directory.Exists(_persistenceOptions.SaveFolder))
                    {
                        _logger.LogDebug("Create session directory {0}", _persistenceOptions.SaveFolder);
                        try
                        {
                            Directory.CreateDirectory(_persistenceOptions.SaveFolder);
                        }
                        catch (IOException ex)
                        {
                            _logger.LogError(ex, "Coundn't create directory {0}", _persistenceOptions.SaveFolder);
                            throw;
                        }
                    }
                    PersistenceOptions.CurrentLogFile = Path.Combine(_persistenceOptions.SaveFolder, userName + ".json");
                }
                else
                {
                    PersistenceOptions.CurrentLogFile = Path.Combine(Files.ExecutablePath, "PersistenceData_" + userName + ".json");
                }

                if (File.Exists(PersistenceOptions.CurrentLogFile))
                {
                    await LoadSessionFile();
                }
                else
                {
                    _logger.LogDebug("No existing session to load : {0}", PersistenceOptions.CurrentLogFile);
                }
            }
            else
            {
                _logger.LogDebug("Persistence disabled");
            }
            return _Session;
        }

        internal PersistenceData SetNewSession(string userContactUrl)
        {
            _logger.LogTrace("SetNewSession()");
            _Session = new PersistenceData()
            {
                UserContactUrl = userContactUrl
            };
            return _Session;
        }

        private void UpdateSessionFromSelenium()
        {
            // get updated selenium session data
            if (!_Session.CookiesInitDate.HasValue)
            {
                _Session.CookiesInitDate = DateTime.UtcNow;
            }
            _Session.Cookies = _seleniumWrapper.Cookies;
            _Session.LocalStorage = _seleniumWrapper.LocalStorage;
            _Session.SessionStorage = _seleniumWrapper.SessionStorage;
        }

        internal void UpdateSeleniumFromSession()
        {
            // set session
            _seleniumWrapper.Cookies = _Session.Cookies; // need to have loaded the page 1st
            _seleniumWrapper.SessionStorage = _Session.SessionStorage; // need to have loaded the page 1st
            _seleniumWrapper.LocalStorage = _Session.LocalStorage; // need to have loaded the page 1st
        }

        /// <summary>
        /// SAVE Task
        /// </summary>
        public async Task RunAsync()
        {
            _logger.LogTrace("RunAsync()");
            if (_persistenceOptions.UsePersistence)
            {
                UpdateSessionFromSelenium();

                await SaveSessionFile();

                _logger.LogDebug("User session saved");
            }
        }
    }
}