using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace IFB
{
    internal class PersistenceManager
    {
        private readonly ILogger<PersistenceManager> _logger;
        private readonly LoggingOptions _loggingOptions;
        private readonly PersistenceOptions _persistenceOptions;

        public PersistenceManager(ILogger<PersistenceManager> logger, IOptions<PersistenceOptions> persistenceOptions, IOptions<LoggingOptions> loggingOptions) // DI : constructor must be public
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("new PersistenceManager()");
            _persistenceOptions = persistenceOptions?.Value ?? throw new ArgumentNullException(nameof(persistenceOptions));
            _loggingOptions = loggingOptions?.Value ?? throw new ArgumentNullException(nameof(loggingOptions));

            // defaut in cas a crash occure before InitFileName() finish and a DumpBrowserContextOnCrash is required
            BaseFileName = Path.Combine(Program.ExecutablePath, "Temp");
        }

        internal string BaseFileName { get; private set; }
        internal PersistenceData Session { get; private set; }
        internal string SessionJsonFile { get; private set; }

        internal async Task LoadPersistence()
        {
            _logger.LogTrace("LoadPersistence()");
            InitFileName();

            if (_persistenceOptions.UsePersistence)
            {
                if (File.Exists(SessionJsonFile))
                {
                    await LoadSessionFile();
                }
                else
                {
                    _logger.LogDebug("No existing session to load : {0}", SessionJsonFile);
                    Session = null;
                }
            }
            else
            {
                _logger.LogDebug("Persistence disabled");
                Session = null;
            }
        }

        internal async Task SaveSessionFile()
        {
            // serialize file
            string content;
            try
            {
                content = JsonConvert.SerializeObject(Session, Formatting.Indented);
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

        internal void SetNewSession(string currentUrl)
        {
            Session = new PersistenceData()
            {
                UserContactUrl = currentUrl
            };
        }

        private void InitFileName()
        {
            // generate the filename base for the .json (session), .png (snapshoot every x sec), or .png and .html (crash dump)
            string tmpUserName = _loggingOptions.User;
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
                tmpSessionBaseFileName = Program.ExecutablePath;
                tmpUserName = string.Concat("PersistenceData_", tmpUserName);
            }
            BaseFileName = Path.Combine(tmpSessionBaseFileName, tmpUserName);
            SessionJsonFile = string.Concat(BaseFileName, ".json");
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
                Session = dataTmp;
            }
            else
            {
                _logger.LogWarning("Persistence limit reached, starting a new session");
            }
        }
    }
}