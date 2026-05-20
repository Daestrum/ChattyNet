using System.Text.Json;
using Chatty.Shared;

namespace GetDateAndTime
{
    public class GetDateAndTime
    {
        public string Name => "get_date_and_time";
        public string Description => @"Returns the JSON wrapped result.";
        public string Schema => @"{
    ""type"": ""object"",
    ""properties"": {},
    ""required"": []
}";
        public string Type => "output";
        public string CanUse => "free";
        public bool Tool => true;

        // NEW: multi‑return metadata
        public int return_count => 2;
        public string return_layout => "date_now, time_now";

        public string Run(string jsonInput)
        {
            var now = DateTime.Now;
            var date = now.ToString("yyyy-MM-dd");
            var time = now.ToString("HH:mm:ss");

            return ToolUtils.WrapResult(return_count,return_layout,date, time);
        }
    }
}
