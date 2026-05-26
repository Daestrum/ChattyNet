using System.Text.Json;

namespace ToolChain
{
    public class ToolChain
    {
        public string Name => "chain_tools";
        public string Description => @"
顺序执行多个工具。每步含 tool_name、args、可选 forward。工具可返回多字段，由 return_count 和 return_layout 定义。若 forward=true，则返回值可作为占位符供下一步用，如 
𝑑𝑎𝑡𝑒、{time}。占位符语法允许并用于步骤间传值。
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
              ""description"": ""true 时，将上一步原始结果注入本步 args 的 'input'。不使用占位符。""
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
        public int return_count => 1;
        public string return_layout => "final_result_of_chain";


        public string Run(string jsonInput)
        {
             return jsonInput;
        }
    }
}
