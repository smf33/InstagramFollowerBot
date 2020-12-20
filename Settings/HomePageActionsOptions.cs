namespace IFB
{
    internal class HomePageActionsOptions : IScrollableActionOptions, ILikeableActionOptions
    {
        internal const string Section = "IFB_HomePageActions";

        public int InitScrools { get; set; }

        public int LikeMax { get; set; }

        public int LikeMin { get; set; }
    }
}