namespace IFB
{
    internal interface IFollowAction : IBotAction
    {
        public bool DoFollow { get; set; }
    }
}