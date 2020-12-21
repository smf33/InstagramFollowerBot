using System;
using System.IO;
using System.Text;
using System.Threading;
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

        private PersistenceData _Session = null;

        #region FileName Management

        private string _sessionBaseFileName = null; // private at GetSessionBaseFileName(string userName)

        private string SessionJsonFile;

        internal string GetSessionBaseFileName(string userName)
        {
            if (string.IsNullOrEmpty(_sessionBaseFileName))
            {
                _logger.LogTrace("GetSessionBaseFileName({0})", userName);

                // generate the filename base for the .json (session), .png (snapshoot every x sec), or .png and .html (crash dump)
                string tmpUserName = userName;
                string tmpSessionBaseFileName = _persistenceOptions.SaveFolder;
                if (!string.IsNullOrWhiteSpace(tmpSessionBaseFileName) && !Directory.Exists(tmpSessionBaseFileName))
                {
                    _logger.LogDebug("Create session directory {0}", tmpSessionBaseFileName);
                    try
                    {
                        Directory.CreateDirectory(tmpSessionBaseFileName);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Coundn't create directory {0}, fallback to executable directory", tmpSessionBaseFileName);
                        tmpSessionBaseFileName = null;
                    }
                }
                if (string.IsNullOrWhiteSpace(tmpSessionBaseFileName))
                {
                    tmpSessionBaseFileName = Files.ExecutablePath;
                    tmpUserName = string.Concat("PersistenceData_", tmpUserName);
                }
                _sessionBaseFileName = Path.Combine(tmpSessionBaseFileName, tmpUserName);
            }
            return _sessionBaseFileName;
        }

        // initied by GetSessionBaseFileName(string userName)
        internal void InitSessionJsonFile(string userName)
        {
            SessionJsonFile = string.Concat(GetSessionBaseFileName(userName), ".json");
        }

        private string GetSessionBaseFileName()
        {
            return GetSessionBaseFileName(string.Concat("Tread", Thread.CurrentThread.ManagedThreadId.ToString()));
        }

        #endregion FileName Management

        public PersistenceAction(ILogger<PersistenceAction> logger, IOptions<PersistenceOptions> persistenceOptions, SeleniumWrapper seleniumWrapper) // DI : constructor must be public

        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new PersistenceAction()");
            _persistenceOptions = persistenceOptions?.Value ?? throw new ArgumentNullException(nameof(persistenceOptions));
            _seleniumWrapper = seleniumWrapper ?? throw new ArgumentNullException(nameof(seleniumWrapper));
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

        #region Dumping

        internal void DumpCurrentPageIfRequired()
        {
            _logger.LogTrace("DumpCurrentPageIfRequired()");

            // do the dump requested
            if (_persistenceOptions.DumpBrowserContextOnCrash)
            {
                string fileNameBase = string.Concat(GetSessionBaseFileName(), ".CrashDump.", DateTime.Now.ToString("yyyyMMdd-HHmmss"));

                _seleniumWrapper.SafeDumpCurrentHtml(string.Concat(fileNameBase, ".html"));

                _seleniumWrapper.SafeDumpCurrentPng(string.Concat(fileNameBase, ".png"));
            }
        }

        #endregion Dumping

        internal async Task<PersistenceData> GetSessionAsync()
        {
            _logger.LogTrace("GetSessionAsync()");
            if (_persistenceOptions.UsePersistence)
            {
                if (File.Exists(SessionJsonFile))
                {
                    await LoadSessionFile();
                }
                else
                {
                    _logger.LogDebug("No existing session to load : {0}", SessionJsonFile);
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

        internal void UpdateSeleniumFromSession()
        {
            // set session
            _seleniumWrapper.Cookies = _Session.Cookies; // need to have loaded the page 1st
            _seleniumWrapper.SessionStorage = _Session.SessionStorage; // need to have loaded the page 1st
            _seleniumWrapper.LocalStorage = _Session.LocalStorage; // need to have loaded the page 1st
        }

        private async Task LoadSessionFile()
        {
            // read raw file
            string content;
            try
            {
                content = await File.ReadAllTextAsync(SessionJsonFile, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Fail to read persistence session data {0} :", SessionJsonFile, ex.GetBaseException().Message);
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
                _logger.LogError(ex, "Fail to deserialize persistence session data {0} : ", SessionJsonFile, ex.GetBaseException().Message);
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
                await File.WriteAllTextAsync(SessionJsonFile, content, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Fail to write persistence session data {0} :", SessionJsonFile, ex.GetBaseException().Message);
                throw;
            }
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
    }
}