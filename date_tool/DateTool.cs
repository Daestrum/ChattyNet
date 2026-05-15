namespace DateTool
{
    public class DateTool : ITool
    {
        public string Name => "get_date";
        public string Description => "Returns the current system date.";
        public string Schema => "{}";
        public ToolType Type => ToolType.Output;
        public string CanUse => "free";

        public string Run(string jsonInput)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            return $"{{ \"date\": \"{date}\" }}";
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


