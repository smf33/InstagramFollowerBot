namespace IFB
{
    internal interface IFollowableAction : IBotAction
    {
        public bool DoFollow { get; set; }
    }
}