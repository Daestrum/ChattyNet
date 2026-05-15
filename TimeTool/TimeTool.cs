using TimeTool;

namespace TimeTool
{
    public class TimeTool : ITool
    {
        public string Name => "get_time";
        public string Description => "Returns the current system time.";
        public string Schema => "{}";
        public ToolType Type => ToolType.Output;
        public string CanUse => "free";

        public string Run(string jsonInput)
        {
            var now = DateTime.Now.ToString("HH:mm:ss");
            return $"{{ \"time\": \"{now}\" }}";
        }
    }
}

