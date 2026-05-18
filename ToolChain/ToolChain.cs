using System.Text.Json;


namespace ToolChain
{
    public class ToolChain
    {
        public string Name => "chain_tools";
        public string Description => "Allows chaining of tool calls into one call. Only testing for now - will not run tools";
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
            try
            {
                // Parse the incoming JSON into our args class
                var args = JsonSerializer.Deserialize<ToolChainArgs>(jsonInput);

                if (args == null || args.Steps == null)
                {
                    //Logger.Info("[tool_chain] No steps provided.");
                    return JsonSerializer.Serialize(new
                    {
                        message = "No steps found in tool chain request."
                    });
                }

                // Log-only processing
                var logResult = RunToolChain(args);

                // Return the logged structure back to Nemo
                return JsonSerializer.Serialize(logResult);
            }
            catch (Exception ex)
            {
                //Logger.Error($"[tool_chain] Error parsing chain: {ex}");
                return JsonSerializer.Serialize(new
                {
                    error = "Failed to parse tool chain request.",
                    details = ex.Message
                });
            }
        }

        public class ToolChainArgs
        {
            public List<ToolChainStep> Steps { get; set; }
        }

        public class ToolChainStep
        {
            public string ToolName { get; set; }
            public JsonElement Args { get; set; }
        }

        public class ToolChainLogResult
        {
            public int StepCount { get; set; }
            public List<object> Steps { get; set; } = new();
        }

        public ToolChainLogResult RunToolChain(ToolChainArgs chain)
        {
            var result = new ToolChainLogResult
            {
                StepCount = chain.Steps?.Count ?? 0
            };

            foreach (var step in chain.Steps)
            {
                result.Steps.Add(new
                {
                    tool = step.ToolName,
                    args = step.Args
                });

                //Logger.write($"[tool_chain] Step: {step.ToolName} Args: {step.Args}");
            }

            return result;
        }
    }
}
