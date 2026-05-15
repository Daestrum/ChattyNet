namespace ToolTemplate
{
    public class Tool : ITool
    {
        public string Name => "place_holder";
        public string Description => "What tool does";

        // Default schema for tools with no parameters
        public string Schema => @"{
                ""type"": ""object"",
                ""properties"": {}
            }";

        public ToolType Type => ToolType.Output;
        public string CanUse => "free";

        public string Run(string jsonInput)
        {
            // TODO: Add tool logic here
            return "{}"; // Always return valid JSON
        }
    }
}


