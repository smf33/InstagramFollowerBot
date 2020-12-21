namespace IFB
{
    internal class LoggingOptions
    {
        internal const string Section = "IFB_Logging";

        public int MakeSnapShootEachSeconds { get; set; }
        public string Password { get; set; }
        public string User { get; set; }
    }
}