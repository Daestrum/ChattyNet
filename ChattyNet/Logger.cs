using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

public static class Logger
{
    private static readonly string LogPath = @"C:\chatty_tools\chatty.log";
    private static readonly BlockingCollection<string> _queue = new();

    static Logger()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath));

        // Background worker that writes logs without blocking UI
        Task.Run(() =>
        {
            foreach (var line in _queue.GetConsumingEnumerable())
            {
                try
                {
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
                catch
                {
                    // Logger must never crash the app
                }
            }
        });
    }

    public static void Write(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        _queue.Add(line);
    }
}


