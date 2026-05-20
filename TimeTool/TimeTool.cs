using Chatty.Shared;

namespace TimeTool
{

    public class TimeTool
    {
        public string Name => "get_time";
        public string Description => "Returns the current system time.";
        public string Schema => "{}";
        public string Type => "output";
        public string CanUse => "free";
        public bool Tool => true;
        public int return_count => 1;
        public string return_layout => "current_time";

        public string Run(string jsonInput)
        {
            var now = DateTime.Now.ToString("HH:mm:ss");
            return ToolUtils.WrapResult(return_count, return_layout, now);
        }
    }
}

