using System;
using System.Collections.Generic;
using System.Text;

namespace TallBoySkim
{
    internal class JavapTool
    {
        public static TallBoySkim.toolResult Run(string jarname)
        {
            var res = new TallBoySkim.toolResult();
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/C ""D:\Jdk27\bin\javap.exe"" -classpath ""{jarname}"" -verbose",
                    WorkingDirectory = TallBoySkim.rootFolder,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var proc = System.Diagnostics.Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                res.exit_code = proc.ExitCode.ToString();
                if (proc.ExitCode != 0 || error != "")
                {
                    res.name = "";
                    res.exit_code = "" + proc.ExitCode;
                }
                else
                {
                    res.name = TallBoySkim.rootFolder + "javap_output.txt";
                    res.exit_code = "0";
                }
            }
            catch (Exception ex)
            {
                res.exit_code = "1";
                res.name = "";
            }
            return res;
        }
    }
}
