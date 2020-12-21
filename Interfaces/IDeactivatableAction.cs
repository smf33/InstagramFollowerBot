namespace IFB
{
    internal interface IDeactivatableAction : IBotAction
    {
        public bool EnableTask { get; set; }
    }
}