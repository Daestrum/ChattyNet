using System;
using System.Collections.Generic;
using System.Text;


namespace TallBoySkim
{
    internal class JarFt
    {
        public static TallBoySkim.toolResult Run(string jarname)
        {
            var res = new TallBoySkim.toolResult();
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"\"D:\\Jdk27\\bin\\jar.exe\" tf {jarname}\"",
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
                    // output to a file and return the path to that file
                    
                    var root = TallBoySkim.rootFolder;
                    
                    var fileName = root + "jarft_output.txt";

                    File.WriteAllText(fileName, output);

                    res.name = fileName;
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
