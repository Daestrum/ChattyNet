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
}


