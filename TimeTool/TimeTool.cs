
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

    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        string Schema { get; }
        ToolType Type { get; }
        string CanUse { get; }
        string Run(string jsonInput);
    }
        public enum ToolType
    {
        Output,
        Action,
        Transform,
        Restricted
    }
}

