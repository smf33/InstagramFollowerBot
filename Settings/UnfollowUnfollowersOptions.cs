namespace IFB
{
    internal class UnfollowUnfollowersOptions
    {
        internal const string Section = "IFB_UnfollowUnfollowers";

        public int CacheFollowersDetectionHours { get; set; }
        public int UnfollowMax { get; set; }
        public int UnfollowMin { get; set; }
    }
}