namespace IFB
{
    internal class LoggingOptions
    {
        internal const string Section = "IFB_Logging";

        public string Password { get; set; }
        public string User { get; set; }

        // little hack for accessing User more easily cross the app
        internal static string CurrentUser { get; set; }
    }
}