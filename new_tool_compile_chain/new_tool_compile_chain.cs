using Chatty.Shared;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace new_tool_compile_chain
{
    public class new_tool_compile_chain
    {
        public string Name => "new_tool_compile_chain";
        public string Description => @"
从 C# 源码创建新工具。

输入：
- tool_name：唯一名称，不可与现有工具重复
- source：单一 C# 文件，只能包含一个 public 类

规则：
- 类名必须与 tool_name 相同
- 至少包含一个 public 方法
- 禁止 Main()、禁止 static 入口点
- 禁止 unsafe、native 导出、P/Invoke
- 禁止使用外部库、NuGet 包
- 禁止多个类、多个文件、复杂结构

流程：
- 创建工具文件夹
- 写入 .cs 与 .csproj
- 编译为 .NET 类库（DLL）
- 安装生成的 DLL

输出：
- success：true/false
- message：编译输出或错误信息
";
        public string Schema => @"{
            ""type"": ""object"",
            ""properties"": {
                ""tool_name"": { ""type"": ""string"" },
                ""source_code"": { ""type"": ""string"" }
            },
            ""required"": [""tool_name"", ""source_code""]
        }";
        public string Type => "output";
        public string CanUse => "cautious use";
        public bool Tool => true;
        public int return_count => 1;
        public string return_layout => "creation_report";

        public string Run(string jsonInput)
        {
            using var doc = JsonDocument.Parse(jsonInput);
            var root = doc.RootElement;

            string toolName = root.GetProperty("tool_name").GetString() ?? "";
            string source = root.GetProperty("source_code").GetString() ?? "";

            toolName = SanitizeName(toolName);
            if (string.IsNullOrWhiteSpace(toolName))
                return Fail("Invalid tool name after sanitisation.");

            // DIRECT CALLS — no registry, no JSON bouncing
            var save = new x_SaveSource();
            var saveResult = save.Run(toolName, source);
            if (!saveResult.Success)
                return Fail("save_source failed", saveResult.Message);

            var compile = new x_CompileSource();
            var compileResult = compile.Run(toolName);
            if (!compileResult.Success)
                return Fail("compile_source failed", compileResult.Message);

            var copy = new x_CopyToLive();
            var copyResult = copy.Run(toolName);
            if (!copyResult.Success)
                return Fail("copy_to_live failed", copyResult.Message);

            // SUCCESS
            var data = new
            {
                status = "success",
                tool_name = toolName,
                message = "Tool created, compiled and deployed successfully."
            };

            string json = JsonSerializer.Serialize(data);
            return ToolUtils.WrapResult(return_count, return_layout, json);
        }

        private string SanitizeName(string name)
            => Regex.Replace(name, "[^A-Za-z0-9_]", "");

        private string Fail(string reason, string detail = "")
        {
            var data = new
            {
                status = "failure",
                reason,
                detail
            };

            string json = JsonSerializer.Serialize(data);
            return ToolUtils.WrapResult(return_count, return_layout, json);
        }
    }
}
