namespace IFB
{
    internal class WaitOptions
    {
        internal const string Section = "IFB_Wait";
        public int PostActionStepMaxWaitMs { get; set; }
        public int PostActionStepMinWaitMs { get; set; }
        public int PostScroolStepMaxWaitMs { get; set; }
        public int PostScroolStepMinWaitMs { get; set; }
        public int PreFollowMaxWaitMs { get; set; }
        public int PreFollowMinWaitMs { get; set; }
        public int PreLikeMaxWaitMs { get; set; }
        public int PreLikeMinWaitMs { get; set; }
        public int WaitTaskMaxWaitMs { get; set; }
        public int WaitTaskMinWaitMs { get; set; }
    }
}