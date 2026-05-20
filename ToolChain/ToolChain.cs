using System.Text.Json;

namespace ToolChain
{
    public class ToolChain
    {
        public string Name => "chain_tools";
        public string Description => @"
Executes multiple tools in sequence. Each step has tool_name, args, and optional forward.
Tools may return multiple named values. The number of returned values is specified by 'return_count' in the tool's schema, and the names of the returned values are specified by 'return_layout'.
When a step has ""forward"": true, the chain runner exposes each returned value as a placeholder for the next step. For example, if a tool returns fields ""date"" and ""time"", the next step may reference them using ${date} and ${time}.
Placeholder syntax IS allowed and is the correct way to pass values between steps.
";
        public string Schema => @"
{
  ""type"": ""object"",
  ""properties"": {
    ""steps"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""tool_name"": { ""type"": ""string"" },
          ""args"": { ""type"": ""object"" },
          ""forward"": {
            ""type"": ""boolean"",
              ""description"": ""If true, chain runner injects the previous step's raw result string into this step's args under the key 'input'. No placeholder syntax is used.""
          }
        },
        ""required"": [""tool_name"", ""args""]
      }
    }
  },
  ""required"": [""steps""]
}
";
        public string Type => "output";
        public string CanUse => "free";
        public bool Tool => true;

        public string Run(string jsonInput)
        {
             return jsonInput;
        }
    }
}
