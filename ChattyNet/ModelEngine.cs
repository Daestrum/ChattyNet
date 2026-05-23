using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ChattyNet
{
    class ModelEngine
    {
        private readonly string model;

        public ModelEngine(string modelName)
        {
            model = modelName;

            RunLms($"load {model}");
            RunLms($"server start --address 127.0.0.1:1234");
            
            MainWindow.Instance.OutputBox.AppendText($"\nModel '{model}' loaded and server starting on port 1234.\n");
            
            for (int i = 0; i < 50; i++)
            {
                if (IsServerReady())
                {
                    MainWindow.Instance.OutputBox.AppendText("Server is ready.\n");
                    break;
                }

                Thread.Sleep(100);
            }
            // tiny wait so server binds to port
            Thread.Sleep(500);
        }

        public void closeModel()
        {
            RunLms("server stop");
            RunLms($"unload {model}");
            MainWindow.Instance.OutputBox.AppendText("\nModel unloaded.\n");
        }
        private static bool IsServerReady()
        {
            string output = RunLms("status");

            return output.Contains("Server: running") &&
                   output.Contains("Model:") &&
                   !output.Contains("loading");
        }
        private static string RunLms(string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "lms",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return output + error;
        }
    }
}
