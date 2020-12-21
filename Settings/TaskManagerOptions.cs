namespace IFB
{
    internal class TaskManagerOptions
    {
        internal const string Section = "IFB_TaskManager";

        public int LoopTaskLimit { get; set; }
        public bool SaveAfterEachAction { get; set; }
        public bool SaveOnEnd { get; set; }
        public bool SaveOnLoop { get; set; }
        public string TaskList { get; set; }
    }
}