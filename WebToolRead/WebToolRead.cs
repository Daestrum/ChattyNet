using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebToolRead
{
    public class web_tool_read : ITool
    {
        public string Name => "web_tool_read";
        public string Description => "Fetches a web page from a given URL and returns readable text content.";
        public string Schema => @"{
    ""type"": ""object"",
    ""properties"": {
        ""url"": { ""type"": ""string"" }
    },
    ""required"": [""url""]
}";
        public ToolType Type => ToolType.Output;
        public string CanUse => "free";

        // Escape a string for safe JSON embedding
        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            s = s.Replace("\\", "\\\\");
            s = s.Replace("\"", "\\\"");
            s = s.Replace("\r\n", "\\n");
            s = s.Replace("\r", "\\r");
            s = s.Replace("\n", "\\n");
            return s;
        }

        private string JsonError(string message)
        {
            string escaped = EscapeJson(message);
            return $"{{\"error\": \"{escaped}\"}}";
        }

        public string Run(string jsonInput)
        {
            try
            {
                string url = ExtractUrl(jsonInput);
                if (string.IsNullOrWhiteSpace(url))
                    return JsonError("Missing or invalid URL");

                string text;
                try
                {
                    text = FetchAndExtract(url).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return JsonError($"Fetch error: {ex.Message}");
                }

                string escaped = EscapeJson(text);
                return $"{{\"text\": \"{escaped}\"}}";
            }
            catch (Exception ex)
            {
                return JsonError($"Unexpected error: {ex.Message}");
            }
        }

        private static string ExtractUrl(string json)
        {
            var match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static async Task<string> FetchAndExtract(string url)
        {
            using var client = new HttpClient();
            var html = await client.GetStringAsync(url);

            // Remove script/style blocks
            html = Regex.Replace(html, "<script[\\s\\S]*?</script>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "<style[\\s\\S]*?</style>", "", RegexOptions.IgnoreCase);

            // Strip HTML tags
            var text = Regex.Replace(html, "<.*?>", " ");

            // Collapse whitespace
            text = Regex.Replace(text, "\\s+", " ").Trim();

            return text;
        }
    }

    // Minimal interface for reference
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        string Schema { get; }
        ToolType Type { get; }
        string CanUse { get; }
        string Run(string jsonInput);
    }

    public enum ToolType { Output }
}

