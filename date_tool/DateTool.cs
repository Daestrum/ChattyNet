namespace DateTool
{
    public class DateTool
    {
        public string Name => "get_date";
        public string Description => "Returns the current system date.";
        public string Schema => "{}";
        public string Type => "output";
        public string CanUse => "free";
        public bool Tool => true;
        public string Run(string jsonInput)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            return $"{{ \"date\": \"{date}\" }}";
        }
    }
}



