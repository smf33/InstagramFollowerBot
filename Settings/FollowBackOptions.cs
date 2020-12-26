namespace IFB
{
    internal class FollowBackOptions : IFollowableOptions
    {
        internal const string Section = "IFB_FollowBack";

        public int FollowMax { get; set; }
        public int FollowMin { get; set; }
    }
}