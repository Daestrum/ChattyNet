using System;
using System.IO;
using System.Text;

namespace ToolReferenceGuide
{
    public class ToolReferenceGuide : ITool
    {
        private const string TemplatePath = "C:\\chatty_tools\\tool_template.txt";

        public string Name => "tool_reference_guide";
        public string Description => "Returns the gold-standard tool template so Nemo can refresh the correct tool structure.";
        public string Schema => @"{
    ""type"": ""object"",
    ""properties"": {},
    ""required"": []
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
                if (!File.Exists(TemplatePath))
                    return JsonError("tool_template.txt not found");

                string content;
                try
                {
                    content = File.ReadAllText(TemplatePath, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    return JsonError($"Read error: {ex.Message}");
                }

                string escaped = EscapeJson(content);
                return $"{{\"template\": \"{escaped}\"}}";
            }
            catch (Exception ex)
            {
                return JsonError($"Unexpected error: {ex.Message}");
            }
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
