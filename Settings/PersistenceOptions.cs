namespace IFB
{
    internal class PersistenceOptions
    {
        internal const string Section = "IFB_Persistence";

        public bool CacheMyContacts { get; set; }
        public string SaveFolder { get; set; }
        public bool UsePersistence { get; set; }
        public int UsePersistenceLimitHours { get; set; }
    }
}