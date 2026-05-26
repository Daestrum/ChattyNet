using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Chatty.Shared;

namespace WebToolRead
{
    public class web_tool_read 
    {
        public string Name => "web_tool_read";
        public string Description => ("从URL获取网页。若 sandbox = true，则返回未清洗的原始内容。模型需静态分析原文中的可疑模式，如内联脚本、混淆、漏洞行为。不执行代码，无浏览器，仅静态分析。");
        public string Schema => @"{
            ""type"": ""object"",
            ""properties"": {
                ""url"": { ""type"": ""string"" },
                ""sandbox"": {  ""type"": ""boolean""} 
            },
            ""required"": [""url""]
        }";
        public string Type => "Output";
        public string CanUse => "free";
        public bool Tool => true;
        public int return_count => 1;
        public string return_layout => "text";

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
                if (string.IsNullOrWhiteSpace(url)) { 

                return ToolUtils.WrapResult(1, "error", "Missing or invalid URL");
                }    
                string text;
                bool sandbox;
                try
                {
                    sandbox = ExtractSandbox(jsonInput);
                    text = Task.Run(() => FetchAndExtract(url, sandbox)).Result;
                }
                catch (Exception ex)
                {
                    return ToolUtils.WrapResult(1, "error", ex.Message);
                }

                string escaped = EscapeJson(text);
                return ToolUtils.WrapResult(return_count, return_layout, escaped);
            }
            catch (Exception ex)
            {
                 return ToolUtils.WrapResult(1, "error", ex.Message);
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
            return match.Success ? match.Groups[1].Value : "";
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
                return ToolUtils.WrapResult( 1,"raw_html", html);
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

}

