namespace IFB
{
    internal class PersistenceOptions
    {
        internal const string Section = "IFB_Persistence";

        public bool CacheMyContacts { get; set; }
        public bool UsePersistence { get; set; }
        public int UsePersistenceLimitHours { get; set; }
        public string SaveFolder { get; set; }

        // little hack for accessing User more easily cross the app
        internal static string CurrentLogFile { get; set; }
    }
}