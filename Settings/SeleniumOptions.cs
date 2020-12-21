namespace IFB
{
    internal class SeleniumOptions
    {
        internal const string Section = "IFB_Selenium";

        public string BrowserArguments { get; set; }
        public string RemoteServer { get; set; }
        public int RemoteServerWarmUpWaitMs { get; set; }
        public float TimeoutSec { get; set; }
        public int WindowMaxH { get; set; }
        public int WindowMaxW { get; set; }
        public int WindowMinH { get; set; }
        public int WindowMinW { get; set; }
    }
}