using Chatty.Shared;
using System.Text.Json;

namespace TallBoySkim
{
    public class TallBoySkim
    {
        public string Name => "java_inspector";
        public string Description => "Returns information on a jar file.";
        public string Schema => @"{
            ""type"": ""object"",
            ""properties"": {
                ""jarname"": { ""type"": ""string"",
                               ""description"": ""Must be a full path not just a name""
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
        
        public static string rootFolder = "";
        public string Run(string jsonInput)
        {
            // stage one: parse input
            if (jsonInput == null)
            {
                exit_code = "1";
                return ToolUtils.WrapResult(2, "Error, Message", exit_code, "No jar file name given");
            }

            var input = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonInput);

            jarname = Path.GetFileName(input["jarname"]);

            rootFolder = Path.GetDirectoryName(jarname) + @"\";

            // stage two: run jar tf
            var res = new toolResult();
            res = JarFt.Run(jarname);

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
                return ToolUtils.WrapResult(2, "Error, Message", exit_code, "Javap tool failed to run");
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
