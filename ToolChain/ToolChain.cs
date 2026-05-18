using System.Text.Json;

namespace ToolChain
{
    public class ToolChain
    {
        public string Name => "chain_tools";
        public string Description => "Allows chaining of tool calls into one call. The result will be a sequential list of tool replies in order of call";
        public string Schema => @"
{
  ""type"": ""object"",
  ""properties"": {
    ""steps"": {
      ""type"": ""array"",
      ""description"": ""A list of tool calls to execute in order."",
      ""items"": {
        ""type"": ""object"",
        ""properties"": {
          ""tool_name"": {
            ""type"": ""string"",
            ""description"": ""The name of the tool to call.""
          },
          ""args"": {
            ""type"": ""object"",
            ""description"": ""Arguments to pass to the tool.""
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
