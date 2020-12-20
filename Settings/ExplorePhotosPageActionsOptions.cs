namespace IFB
{
    internal class ExplorePhotosPageActionsOptions : IScrollableActionOptions, IFollowableActionOptions, ILikeableActionOptions
    {
        internal const string Section = "IFB_ExplorePhotosPageActions";

        public int InitScrools { get; set; }
        public int FollowMax { get; set; }
        public int FollowMin { get; set; }
        public int LikeMax { get; set; }
        public int LikeMin { get; set; }
    }
}