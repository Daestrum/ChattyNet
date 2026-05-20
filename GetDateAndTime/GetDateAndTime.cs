using System.Text.Json;

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

            return WrapResult(date, time);
        }
        string WrapResult(params string[] data)
        {
            // Split the return_layout into field names
            var names = return_layout.Split(',')
                                     .Select(n => n.Trim())
                                     .ToArray();

            // Safety check
            if (names.Length != data.Length)
                throw new Exception($"Tool metadata mismatch: return_layout has {names.Length} fields but tool returned {data.Length} values.");

            // Build dictionary
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < names.Length; i++)
                dict[names[i]] = data[i];

            // Serialize to JSON
            return JsonSerializer.Serialize(dict);
        }

    }
}
