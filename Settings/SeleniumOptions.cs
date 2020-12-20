namespace IFB
{
    internal class SeleniumOptions
    {
        internal const string Section = "IFB_Selenium";

        public float TimeoutSec { get; set; }
        public int RemoteServerWarmUpWaitMs { get; set; }
        public int WindowMaxH { get; set; }
        public int WindowMaxW { get; set; }
        public int WindowMinH { get; set; }
        public int WindowMinW { get; set; }
        public string BrowserArguments { get; set; }
        public string RemoteServer { get; set; }
        public bool DumpBrowserContextOnCrash { get; set; }
    }
}