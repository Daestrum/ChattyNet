using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
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
                // if no main to use - skip javap and return empty result with exit code 0
                if (string.IsNullOrWhiteSpace(TallBoySkim.FirstClassForJavap))
                {
                    res.exit_code = "1";
                    res.name = "Javap Error: No class name supplied";
                    return res;
                }
                var argCheck = $@"/C ""D:\Jdk27\bin\javap.exe"" -public -classpath "".\{jarname}"" ""{TallBoySkim.FirstClassForJavap}""";

                var psi = new ProcessStartInfo
                {
                    FileName = "D:\\Jdk27\\bin\\javap.exe",
                    Arguments = $"-public -classpath \"{jarname}\" \"{TallBoySkim.FirstClassForJavap}\"",
                    WorkingDirectory = TallBoySkim.RootFolder,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var proc = Process.Start(psi);
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                res.exit_code = proc.ExitCode.ToString();

                if (proc.ExitCode != 0)
                {
                    res.name = $"Javap failed:\nSTDERR:\n{stderr}";
                }
                else
                {
                    var fileName = Path.Combine(TallBoySkim.RootFolder, "javap_output.txt");
                    File.WriteAllText(fileName, stdout + "\n\n# STDERR (warnings):\n" + stderr);
                    res.name = fileName;
                }

            }
            catch (Exception ex)
            {
                res.exit_code = "1";
                res.name = $"Error: failed in catch {ex.Message}";
            }
            return res;
        }
    }
}
