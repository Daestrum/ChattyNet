using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;


namespace ChattyNet
{
    public static class ToolRefresher
    {
        public static string _folder;
        private static int _intervalMs;
        private static CancellationTokenSource _cts;
               // Tracks last known state
        private static Dictionary<string, (DateTime Timestamp, string Hash)> _known =
            new Dictionary<string, (DateTime, string)>();

        public static List<string> NewTools = new();
        public static List<string> UpdatedTools = new();
        public static List<string> RemovedTools = new();
        
        public class RefreshBatch
        {
            public List<string> NewTools = new();
            public List<string> UpdatedTools = new();
            public List<string> RemovedTools = new();
        }

        public static void Initialize(string folder, int interval = 5000)
        {
            _folder = folder;

            if (interval < 1000)
            {
                _intervalMs = interval * 1000; // Convert seconds to ms if too small
            }
            else
            {
                _intervalMs = interval;
            }
        }

        public static void Start()
        {
            if (_cts != null)
                return;

            _cts = new CancellationTokenSource();
            Task.Run(() => Loop(_cts.Token));
        }

        public static void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private static async Task Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    ScanFolder();
                }
                catch (Exception ex)
                {
                    Logger.Write("Refresher error: " + ex.Message);
                }

                await Task.Delay(_intervalMs, token);
            }
        }

        private static void ScanFolder()
        {
            NewTools.Clear();
            UpdatedTools.Clear();
            RemovedTools.Clear();

            RefreshBatch batch = new RefreshBatch();

            if (!Directory.Exists(_folder))
                return;

            var dlls = Directory.GetFiles(_folder, "*.dll");

            // Check for new or updated DLLs
            foreach (var dll in dlls)
            {
                var ts = File.GetLastWriteTimeUtc(dll);

                if (!_known.TryGetValue(dll, out var entry))
                {
                    // NEW DLL
                    var name = Path.GetFileNameWithoutExtension(dll);
                    Logger.Write($"[Refresher] NEW tool detected: {name}");
                    NewTools.Add(name);
                    _known[dll] = (ts, ComputeHash(dll));
                    batch.NewTools.Add(name);
                    continue;
                }

                if (ts != entry.Timestamp)
                {
                    // Timestamp changed → check hash
                    var newHash = ComputeHash(dll);

                    if (newHash != entry.Hash)
                    {
                        var name = Path.GetFileNameWithoutExtension(dll);
                        Logger.Write($"[Refresher] UPDATED tool detected: {name}");
                        UpdatedTools.Add(name);
                        batch.UpdatedTools.Add(name);
                        _known[dll] = (ts, newHash);
                    }
                    else
                    {
                        Logger.Write($"[Refresher] Timestamp changed but hash same: {Path.GetFileName(dll)}");
                        _known[dll] = (ts, newHash);
                    }
                }
            }

            // Check for removed DLLs
            foreach (var knownDll in _known.Keys.ToList())
            {
                if (!dlls.Contains(knownDll))
                {
                    var name = Path.GetFileNameWithoutExtension(knownDll);
                    Logger.Write($"[Refresher] REMOVED tool detected: {name}");
                    RemovedTools.Add(name);
                    batch.RemovedTools.Add(name);    
                    _known.Remove(knownDll);
                }
            }

            DLLStore.Instance.ApplyChanges2(batch);

        }

        private static string ComputeHash(string file)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(file);
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}

