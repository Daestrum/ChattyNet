using System;
using System.Collections.Generic;
using System.Reflection;
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
                    Arguments = $"/C \"\"D:\\Jdk27\\bin\\jar.exe\" tf {jarname} | findstr /i .class\"",
                    WorkingDirectory = TallBoySkim.RootFolder,
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
                    
                    var root = TallBoySkim.RootFolder;

                    var fileName = Path.Combine(root, "jarft_output.txt");

                    var lines = output.Split('\n');
                    
                    var filtered = lines
                        .Where(l => !l.Contains("anywheresoftware", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    // Step 2: find a good candidate class
                    var cleaned = filtered
                        .Select(l => l.Trim())   // <-- critical
                        .Where(l => l.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    string? mainClass = cleaned
                        .FirstOrDefault(l => l.EndsWith("main.class", StringComparison.OrdinalIgnoreCase))
                        ?? cleaned.FirstOrDefault();

                    if (mainClass != null)
                    {
                        mainClass = mainClass
                            .Replace('/', '.')
                            .Replace('\\', '.')
                            .Replace("\r", "")
                            .Replace("\n", "")
                            .Replace(".class", "", StringComparison.OrdinalIgnoreCase)
                            .Trim();

                        TallBoySkim.FirstClassForJavap = mainClass;
                    }
                    output = string.Join("\n", filtered);

                    File.WriteAllText(fileName, output);
                    // shrink to json for model input

                    var dict = new Dictionary<string, List<string>>();

                    foreach (var entry in cleaned)
                    {
                        int lastSlash = entry.LastIndexOf('/');
                        if (lastSlash < 0)
                            continue;

                        string folder = entry.Substring(0, lastSlash);
                        string cls = entry.Substring(lastSlash + 1);
                        cls = cls.Replace(".class", "", StringComparison.OrdinalIgnoreCase);

                        if (!dict.TryGetValue(folder, out var list))
                        {
                            list = new List<string>();
                            dict[folder] = list;
                        }

                        list.Add(cls);
                    }

                    string json = System.Text.Json.JsonSerializer.Serialize(
                        dict,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                    );
                    var fname = Path.Combine(root, "jarft_compact.json");
                    
                    File.WriteAllText(fname, json);



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
