
namespace TimeTool
{

    public class TimeTool
    {
        public string Name => "get_time";
        public string Description => "Returns the current system time.";
        public string Schema => "{}";
        public string Type => "output";
        public string CanUse => "free";

        public string Run(string jsonInput)
        {
            var now = DateTime.Now.ToString("HH:mm:ss");
            return $"{{ \"time\": \"{now}\" }}";
        }
    }

    

}

