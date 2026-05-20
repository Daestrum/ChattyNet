using System;
using System.IO;
using System.Text;
using Chatty.Shared;

namespace ToolReferenceGuide
{
    public class ToolReferenceGuide 
    {
        private const string TemplatePath = "C:\\chatty_tools\\tool_template.txt";

        public string Name => "tool_reference_guide";
        public string Description => "Returns the gold-standard tool template so Nemo can refresh the correct tool structure.";
        public string Schema => @"{
            ""type"": ""object"",
            ""properties"": {},
            ""required"": []
        }";
        public string Type => "output";
        public string CanUse => "free";
        public bool Tool => true;
        public int return_count => 1;
        public string return_layout => "template";

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
                    return ToolUtils.WrapResult(1,"error","tool_template.txt not found");

                string content;
                try
                {
                    content = File.ReadAllText(TemplatePath, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    return ToolUtils.WrapResult(1,"error",ex.Message);
                }

                string escaped = EscapeJson(content);
                return ToolUtils.WrapResult(return_count,return_layout,escaped);
            }
            catch (Exception ex)
            {
                return ToolUtils.WrapResult(1,"erreor", ex.Message);
            }
        }
    }


}
