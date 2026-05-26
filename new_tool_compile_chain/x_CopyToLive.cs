using System;
using System.IO;

namespace new_tool_compile_chain
{
    internal class x_CopyToLive
    {
        public CopyToLiveResult Run(string toolName)
        {
            string toolPath = $"D:\\nemo_tools\\{toolName}";
            string dllPath = $"{toolPath}\\bin\\Release\\net10.0\\{toolName}.dll";

            try
            {
                // Ensure the DLL exists
                if (!File.Exists(dllPath))
                {
                    return new CopyToLiveResult
                    {
                        Success = false,
                        Message = "Release DLL not found. Did the build succeed?"
                    };
                }

                // Ensure destination exists
                Directory.CreateDirectory("C:\\chatty_tools");

                // Copy to live tools folder
                File.Copy(dllPath, $"C:\\chatty_tools\\{toolName}.dll", true);
            }
            catch (Exception ex)
            {
                return new CopyToLiveResult
                {
                    Success = false,
                    Message = ex.Message.Trim()
                };
            }

            return new CopyToLiveResult
            {
                Success = true,
                Message = "Copy to live successful"
            };
        }
    }

    internal class CopyToLiveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
