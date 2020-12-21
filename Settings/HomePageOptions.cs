namespace IFB
{
    internal class HomePageOptions : IScrollableActionOptions, ILikeableOptions
    {
        internal const string Section = "IFB_HomePage";

        public int InitScrools { get; set; }

        public int LikeMax { get; set; }

        public int LikeMin { get; set; }
    }
}