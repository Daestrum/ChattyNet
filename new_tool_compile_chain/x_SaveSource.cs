namespace new_tool_compile_chain
{
    internal class x_SaveSource
    {
        public SaveResult Run(string toolName, string sourceCode)
        {
            try
            {
                Console.WriteLine($"Saving source code for tool '{toolName}'...");

                // sanitize name
                if (toolName.Contains("..") || toolName.Contains("/") || toolName.Contains("\\"))
                {
                    return new SaveResult
                    {
                        Success = false,
                        Message = "Tool name cannot contain path traversal characters. -1 Brownie points"
                    };
                }

                var dir = $"D:\\nemo_tools\\{toolName}";
               
                Directory.CreateDirectory(dir);

                File.WriteAllText($"{dir}\\{toolName}.cs", sourceCode);

                var projectFile =
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
                File.WriteAllText($"{dir}\\{toolName}.csproj", projectFile);

                return new SaveResult
                {
                    Success = true,
                    Message = "Source saved"
                };
            }
            catch (Exception ex)
            {
                return new SaveResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }

    internal class SaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}

