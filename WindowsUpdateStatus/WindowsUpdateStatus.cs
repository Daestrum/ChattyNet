using Chatty.Shared;
using System.Diagnostics;

namespace WindowsUpdateStatus
{
    public class WindowsUpdateStatus
    {
        public string Name => "windows_update_check"; //tool name, should be unique across all tools
        public string Description => "Returns info on pending updates."; //description of the tool, should be concise and informative
        public string Schema => "{}"; //json schema for the input, should be a valid json schema, if no input needed, use empty json object
        public string Type => "output"; //type of the tool, can be "input" or "output", input means the tool will receive input from user, output means the tool will return output to user
        public string CanUse => "free"; //can use condition, can be "free", "paid", "restricted", free means anyone can use, paid means only paid users can use, restricted means only specific users can use
        public bool Tool => true; //whether this is a tool, should be true for all tools
        public int return_count => 1; //number of return values, should be a positive integer
        public string return_layout => "update_info"; //layout of the return values, should be a string that describes the return values, can be used to format the output, for example, if return_count is 2 and return_layout is "name, age", then the output will be "name: value1, age: value2"

        public string Run(string jsonInput)
        {
            try
            {
                // PowerShell to get pending updates (read-only)
                string psPending = @"
            Get-WindowsUpdate -MicrosoftUpdate -IgnoreUserInput -AcceptAll -ErrorAction SilentlyContinue |
            Select-Object Title, Size, MsrcSeverity |
            ConvertTo-Json
        ";

                // PowerShell to get installed updates (read-only)
                string psInstalled = @"
            Get-WmiObject -Class Win32_QuickFixEngineering |
            Select-Object HotFixID, InstalledOn |
            ConvertTo-Json
        ";

                var pending = RunPowerShell(psPending);
                var installed = RunPowerShell(psInstalled);

                // Build a JSON object for Nemo
                var result = new
                {
                    pending_updates = pending,
                    installed_updates = installed
                };

                string json = System.Text.Json.JsonSerializer.Serialize(result);

                return ToolUtils.WrapResult(return_count, return_layout, json);
            }
            catch (Exception ex)
            {
                return ToolUtils.WrapResult(return_count, return_layout, $"{{\"error\":\"{ex.Message}\"}}");
            }
        }
        private static string RunPowerShell(string script)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output.Trim();
        }

    }
}
