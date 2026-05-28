using Chatty.Shared;
using System.Text.Json;

namespace TallBoySkim
{
    public class TallBoySkim
    {
        public string Name => "java_inspector";
        public string Description => "用于分析 JAR 文件。要求用户先把 JAR 放在正确目录中。工具会读取该目录下的 JAR 并返回其内容信息。";
        public string Schema => @"{
    ""type"": ""object"",
    ""properties"": {
        ""jarname"": { ""type"": ""string"",
                       ""description"": ""必须是文件名（如 file.jar），不含路径""
                     }
    },
    ""required"": [""jarname""]
}";

        public string Type => "output";
        public string CanUse => "on-request";
        public bool Tool => true;
        public int return_count => 4;
        public string return_layout => "return_jarft, return_jdeps, return_javap, return_exitcode";

        private string jarname = "";
        private string jarft_name = "";
        private string jdeps_name = "";
        private string javap_name = "";
        private string exit_code = "";

        // SAFE: static property, not mutated by appending
        public static string RootFolder { get; private set; } = @"c:\java-inspect\";

        public static string? FirstClassForJavap = null;

        public string Run(string jsonInput)
        {
            // stage one: parse input
            if (jsonInput == null)
            {
                exit_code = "1";
                return ToolUtils.WrapResult(2, "Error, Message", exit_code, "No jar file name given");
            }

            var input = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonInput);

            // normalise jar name
            var jarnameNoExt = Path.GetFileNameWithoutExtension(input["jarname"]);
            jarname = jarnameNoExt + ".jar";

            // build safe folder path
            var baseFolder = @"c:\java-inspect\";
            RootFolder = Path.Combine(baseFolder, jarnameNoExt);

            // ensure folder exists
            if (!Directory.Exists(RootFolder))
            {
                exit_code = "1";
                return ToolUtils.WrapResult(2, "Error, Message", exit_code,
                    $"Folder '{RootFolder}' does not exist. Place the jar file there first.");
            }
            // check for exactly one jar file
            var jars = Directory.GetFiles(RootFolder, "*.jar");
            if (jars.Length == 0)
            {
                exit_code = "1";
                return ToolUtils.WrapResult(2, "Error, Message", exit_code, "No jar file found in folder");
            }
            if (jars.Length > 1)
            {
                exit_code = "1";
                return ToolUtils.WrapResult(2, "Error, Message", exit_code, "Multiple jar files found in folder");
            }
            // stage two: run jar tf
                var res = JarFt.Run(jarname);
            if (res.exit_code != "0")
            {
                exit_code = res.exit_code;
                return ToolUtils.WrapResult(2, "Error, Message", exit_code, "Jar FT tool failed to run");
            }
            jarft_name = res.name;

            // stage three: run jdeps
            res = JdepsTool.Run(jarname);
            if (res.exit_code != "0")
            {
                exit_code = res.exit_code;
                return ToolUtils.WrapResult(2, "Error, Message", exit_code, "Jdeps tool failed to run");
            }
            jdeps_name = res.name;

            // stage four: run javap


            res = JavapTool.Run(jarname);
            if (res.exit_code != "0")
            {
                exit_code = res.exit_code;
                return ToolUtils.WrapResult(2, "Error, Message", exit_code, res.name);
            }
            javap_name = res.name;

            // final return
            return ToolUtils.WrapResult(return_count, return_layout, jarft_name, jdeps_name, javap_name, exit_code);
        }

        public class toolResult
        {
            public string name { get; set; }
            public string exit_code { get; set; }
        }
    }
}
