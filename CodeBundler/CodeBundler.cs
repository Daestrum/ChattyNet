using Chatty.Shared;
using System.Diagnostics;
using System.Text.Json;

namespace CodeBundler
{
    public class CodeBundler
    {
        public string Name => "code_bundler";
        public string Description =>
           "用 CodeBundle.jar 处理 .b4j 文件并生成 JSON。";
            //"Runs the B4X CodeBundle.jar tool using a specified .b4j file and output JSON path.";

        public string Schema =>
@"{
  ""type"": ""object"",
  ""properties"": {
    ""project_dir"": {
      ""type"": ""string"",
      ""description"": ""要处理的 .b4j 文件完整路径""
    },
    ""output_Json"": {
      ""type"": ""string"",
      ""description"": ""生成的 JSON 输出路径""
    }
  },
  ""required"": [""project_dir"", ""output_Json""]
}
";

 /*       @"{
            ""type"": ""object"",
            ""properties"": {
                ""project_dir"": { ""type"": ""string"", ""description"": ""Full path to the .b4j file"" },
                ""output_Json"": { ""type"": ""string"", ""description"": ""Full path to the output JSON file"" }
            },
            ""required"": [""project_dir"", ""output_Json""]
        }";*/

        public string Type => "output";
        public string CanUse => "free";
        public bool Tool => true;

        public int return_count => 1;
        public string return_layout => "bundle_json";

        public string Run(string jsonInput)
        {
            try
            {
                // Parse input
                var doc = JsonDocument.Parse(jsonInput);
                var root = doc.RootElement;

                // These are now EXACT paths, no scanning
                string b4jFile = root.GetProperty("project_dir").GetString();
                string outputJson = root.GetProperty("output_Json").GetString();

                // Fixed JDK path
                string javaExe = @"D:\jdk27\bin\java.exe";
                string bundlerJar = @"C:\temp\CodeBundle.jar";

                // Build process
                var psi = new ProcessStartInfo
                {
                    FileName = javaExe,
                    Arguments = $"-jar \"{bundlerJar}\" \"{b4jFile}\" \"{outputJson}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                proc.WaitForExit();

                string stderr = proc.StandardError.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(stderr))
                    return ToolUtils.WrapResult(1, "error", stderr);

                if (!File.Exists(outputJson))
                    return ToolUtils.WrapResult(1, "error", $"JSON not found: {outputJson}");

                string json = File.ReadAllText(outputJson);

                return ToolUtils.WrapResult(return_count, return_layout, json);
            }
            catch (Exception ex)
            {
                return ToolUtils.WrapResult(1, "error", ex.Message);
            }
        }
    }
}
