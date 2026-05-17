using ChattyNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using static ChattyNet.ToolRefresher;

namespace ChattyNet
{
    public class DLLStore
    {
        public static DLLStore Instance { get; } = new DLLStore();

        class LiveDllEntry
        {
            public byte[] Bytes { get; set; }
            public DateTime Timestamp { get; set; }
            public string Name { get; set; }
            public object Alc { get; set; }
            public object Assembly { get; set; }
            public object Instance { get; set; }
            public ChangeFlag flag { get; set; }  // New, Modified, Removed, None
            public bool Dirty { get; internal set; }
        }
        class ReserveDllEntry
        {
            public byte[] Bytes { get; set; }
            public DateTime Timestamp { get; set; }
            public string Name { get; set; }
            public bool Dirty { get; internal set; }
        }
        public enum ChangeFlag
        {
            None,
            New,
            Modified,
            Removed
        }

       
        private int _maxLiveSize = 10;
        public bool LiveIsDirty => LiveDllStore.Values.Any(e => e.Dirty);
        public bool ReserveIsDirty => ReserveDllStore.Values.Any(e => e.Dirty);

        Dictionary<string, LiveDllEntry> LiveDllStore = new();
        Dictionary<string, ReserveDllEntry> ReserveDllStore = new();

        public DLLStore()
        {
            
        }
        public void ReadAllTools(string folder)
        {
            foreach (var path in Directory.GetFiles(folder, "*.dll"))
            {
                // 1. Read bytes
                byte[] bytes = File.ReadAllBytes(path);
                // 2. Get timestamp
                DateTime ts = File.GetLastWriteTime(path);
                // 3. Extract real assembly name from bytes
                string name = GetAssemblyName(bytes);
                // 4. Store in LIVE or RESERVE depending on capacity
                if (LiveDllStore.Count < _maxLiveSize)
                {
                    LiveDllStore[name] = new LiveDllEntry
                    {
                        Bytes = bytes,
                        Timestamp = ts
                    };
                }
                else
                {
                    ReserveDllStore[name] = new ReserveDllEntry
                    {
                        Bytes = bytes,
                        Timestamp = ts
                    };
                }
            }
        }
        public void ApplyChanges(RefreshBatch batch)
        {
            foreach (var name in batch.NewTools)
                Logger.Write($"  NEW: {name}");
            foreach (var name in batch.UpdatedTools)
                Logger.Write($"  UPDATED: {name}");
            foreach (var name in batch.RemovedTools)
                Logger.Write($"  REMOVED: {name}");

            foreach (var name in batch.NewTools)
                MarkEntry(name, ChangeFlag.New);

            foreach (var name in batch.UpdatedTools)
                MarkEntry(name, ChangeFlag.Modified);

            foreach (var name in batch.RemovedTools)
                MarkEntry(name, ChangeFlag.Removed);
        }

        private void MarkEntry(string name, ChangeFlag flag)
        {
            if (!LiveDllStore.TryGetValue(name, out var entry))
            {
                entry = new LiveDllEntry();
                LiveDllStore[name] = entry;
            }

            entry.flag = flag;
            entry.Dirty = true;
        }

        public string GetNewToolSchema()
        {
            var sb = new StringBuilder();
            sb.Append("[\n");

            bool first = true;

            foreach (var entry in LiveDllStore.Values)
            {
                if (entry.flag == ChangeFlag.Removed)
                    continue;

                var inst = entry.Instance;
                if (inst == null)
                    continue;

                var type = inst.GetType();

                // Read metadata via reflection
                string name = type.GetProperty("Name")?.GetValue(inst)?.ToString();
                string description = type.GetProperty("Description")?.GetValue(inst)?.ToString();
                string schemaJson = type.GetProperty("Schema")?.GetValue(inst)?.ToString();
                string toolType = type.GetProperty("Type")?.GetValue(inst)?.ToString();
                string canUse = type.GetProperty("CanUse")?.GetValue(inst)?.ToString();

                if (!first)
                    sb.Append(",\n");

                first = false;

                sb.Append("  {\n");
                sb.AppendFormat("    \"name\": \"{0}\",\n", name);
                sb.AppendFormat("    \"description\": \"{0}\",\n", description);
                sb.AppendFormat("    \"parameters\": {0},\n", schemaJson);
                sb.AppendFormat("    \"canUse\": \"{0}\",\n", canUse);
                sb.AppendFormat("    \"type\": \"{0}\"\n", toolType);
                sb.Append("  }");
            }

            sb.Append("\n]");
            return sb.ToString();
        }


        public void debugToolStore()
        {
            Logger.Write("LIVE DLLs: {LiveDllStore.Count}\n");
            foreach (var kvp in LiveDllStore)
            {
                Logger.Write($"LIVE - {kvp.Key} (Timestamp: {kvp.Value.Timestamp})\n");
            }
            Logger.Write("\nRESERVE DLLs: {ReserveDllStore.Count} \n");
            foreach (var kvp in ReserveDllStore)
            {
                Logger.Write($"RESERVE - {kvp.Key} (Timestamp: {kvp.Value.Timestamp})\n");
            }
        }

        public string GetAssemblyName(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var peReader = new PEReader(stream);

            var reader = peReader.GetMetadataReader();
            var def = reader.GetAssemblyDefinition();
            var name = reader.GetString(def.Name);

            return name;
        }

        public void Demote(string name)
        {
            var live = LiveDllStore[name];
            ReserveDllStore[name] = new ReserveDllEntry
            {
                Bytes = live.Bytes,
                Timestamp = live.Timestamp
            };
            LiveDllStore.Remove(name);
        }

        public void Promote(string name)
        {
            var reserve = ReserveDllStore[name];
            LiveDllStore[name] = new LiveDllEntry
            {
                Bytes = reserve.Bytes,
                Timestamp = reserve.Timestamp
            };

            ReserveDllStore.Remove(name);
        }
        public void Swap(string name1, string name2)
        {
            bool name1IsLive = LiveDllStore.ContainsKey(name1);
            if (name1IsLive)
            {
                Demote(name1);
                Promote(name2);
            }
            else
            {
                Promote(name1);
                Demote(name2);
            }
        }
    }
}