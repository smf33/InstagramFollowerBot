namespace IFB
{
    internal class ExplorePhotosOptions : IScrollableActionOptions, IFollowableActionOptions, ILikeableActionOptions
    {
        internal const string Section = "IFB_ExplorePhotos";

        public int InitScrools { get; set; }
        public int FollowMax { get; set; }
        public int FollowMin { get; set; }
        public int LikeMax { get; set; }
        public int LikeMin { get; set; }
    }
}