using System;
using System.Diagnostics;
using System.IO;

namespace new_tool_compile_chain
{
    internal class x_CompileSource
    {
        public CompileResult Run(string toolName)
        {
            string toolsDir = $"D:\\nemo_tools\\{toolName}\\";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C \"\"C:\\Program Files\\dotnet\\dotnet.exe\" build --configuration Release\"",
                    WorkingDirectory = toolsDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };



                var proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    return new CompileResult
                    {
                        Success = false,
                        Message = error.Length > 0 ? error : output
                    };
                }
            }
            catch (Exception ex)
            {
                return new CompileResult
                {
                    Success = false,
                    Message = ex.Message.Trim()
                };
            }

            return new CompileResult
            {
                Success = true,
                Message = "Compilation successful +1 Brownie points"
            };
        }
    }

    internal class CompileResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
