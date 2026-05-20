using System.Text.Json;
using Chatty.Shared;

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
        public int return_count => 1;
        public string return_layout => "current_date";

        public string Run(string jsonInput)
        {
            var date = DateTime.Now.ToString("dd-MM-yyyy");
            return ToolUtils.WrapResult(return_count, return_layout, date);
        }

    }
}



