using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebToolRead
{
    public class web_tool_read : ITool
    {
        public string Name => "web_tool_read";
        public string Description => ("Fetches a web page from a given URL. If \"sandbox\": true, return the raw page content with no cleaning. The model should then analyse the raw text for suspicious or unsafe patterns (e.g., inline scripts, obfuscation, exploit‑like behaviour). No code is executed. No browser is involved. Analysis is static only.");
        public string Schema => @"{
    ""type"": ""object"",
    ""properties"": {
        ""url"": { ""type"": ""string"" },
        ""sandbox"": {  ""type"": ""boolean""} 
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
                bool sandbox;
                try
                {
                    sandbox = ExtractSandbox(jsonInput);
                    text = Task.Run(() => FetchAndExtract(url, sandbox)).Result;
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

        private static bool ExtractSandbox(string json)
        {
            var match = Regex.Match(json, "\"sandbox\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            return match.Success && match.Groups[1].Value.ToLower() == "true";
        }

        private static string ExtractUrl(string json)
        {
            var match = Regex.Match(json, "\"url\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static async Task<string> FetchAndExtract(string url, bool sandbox)
        {
            using var client = new HttpClient();
            
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                    "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36"
            );
            
            var html = await client.GetStringAsync(url);

            if (sandbox)
            {
                // return html;
                return JsonSerializer.Serialize(new { rawHTML = html });
            }

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

