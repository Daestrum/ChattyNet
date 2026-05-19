using Chatty.Shared;
using ChattyNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static ChattyNet.ToolRefresher;

namespace ChattyNet
{
    public class DLLStore
    {
        public static DLLStore Instance { get; } = new DLLStore();

        public class LiveDllEntry
        {
            public byte[] Bytes { get; set; }
            public DateTime Timestamp { get; set; }
            public string Name { get; set; }
            public AssemblyLoadContext Alc { get; set; }
            public Assembly Assembly { get; set; }
            public object Instance { get; set; }
            public ChangeFlag flag { get; set; }  // New, Modified, Removed, None
            public bool Dirty { get; internal set; }
        }
        public class ReserveDllEntry
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
        public sealed class ToolProbeResult
        {
            public bool IsTool { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Schema { get; set; }
            public string Type { get; set; }
            public string CanUse { get; set; }
        }
        public class ToolSchema
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            // Keep this flexible – it's arbitrary JSON
            [JsonPropertyName("parameters")]
            public JsonElement Parameters { get; set; }

            [JsonPropertyName("canUse")]
            public string CanUse { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }
        }
        private bool _showDebug = false;

        private int _maxLiveSize = 10;
        public bool LiveIsDirty => LiveDllStore.Values.Any(e => e.Dirty);
        public bool ReserveIsDirty => ReserveDllStore.Values.Any(e => e.Dirty);

        public Dictionary<string, string> DllNameToToolName = new();
        public Dictionary<string, string> ToolNameToDllName = new();


        public string _lastToolSpecJson = "";
        private string _cachedToolSpecJson = "";


        public Dictionary<string, LiveDllEntry> LiveDllStore = new();
        public Dictionary<string, ReserveDllEntry> ReserveDllStore = new();

         public List<object> ConvertSchemaToToolList(string schemaJson)
        {
            var schema = JsonSerializer.Deserialize<List<ToolSchema>>(schemaJson)
                         ?? new List<ToolSchema>();

            var list = new List<object>();

            foreach (var s in schema)
            {
                list.Add(new
                {
                    type = "function",
                    function = new
                    {
                        name = s.Name,
                        description = s.Description,
                        // Pass parameters through as raw JSON
                        parameters =
                            s.Parameters.ValueKind == JsonValueKind.Undefined ||
                            s.Parameters.ValueKind == JsonValueKind.Null ||
                            s.Parameters.ValueKind == JsonValueKind.Object && s.Parameters.EnumerateObject().Count() == 0
                                ? JsonDocument.Parse("{\"type\":\"object\",\"properties\":{}}").RootElement
                                : s.Parameters
                    }
                });
            }

            return list;
        }


        public void ApplyChanges(RefreshBatch batch)
        {
            bool anyChanges = false;
            
            if (MainWindow.Instance._isToolInUse) return; 
            
            // 1. Handle NEW tools
            foreach (var name in batch.NewTools)
            {
                Logger.Write($"  NEW: {name}");
                ReadDllFromDisk(name);          // creates LiveDllEntry with bytes + timestamp
                MarkEntry(name, ChangeFlag.New);
                anyChanges = true;
                BuildDllChain(name);              // loads assembly, creates instance, etc.
            }

            // 2. Handle UPDATED tools
            foreach (var name in batch.UpdatedTools)
            {
                Logger.Write($"  UPDATED: {name}");

                // 1. Unload old ALC if present
                if (LiveDllStore.TryGetValue(name, out var live))
                {
                    try
                    {
                        live.Alc?.Unload();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    catch { }
                }

                // 2. Remove old live entry
                LiveDllStore.Remove(name);

                // 3. DO NOT delete from DB (updates reuse DB entry)

                // 4. Load new bytes (DB-aware)
                ReadDllFromDisk(name);

                // 5. Mark as modified
                MarkEntry(name, ChangeFlag.Modified);

                // 6. Rebuild chain (loads assembly)
                BuildDllChain(name);

                anyChanges = true;
            }

            // 3. Handle REMOVED tools
            foreach (var name in batch.RemovedTools)
            {
                Logger.Write($"  REMOVED: {name}");
                if (LiveDllStore.TryGetValue(name, out var live))
                {
                    try
                    {
                        live.Alc?.Unload();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    catch { }
                }
                LiveDllStore.Remove(name);   // <-- REMOVE IT
                DBDllStore.Delete(name);          // <-- DELETE FROM DB
                anyChanges = true;
            }


            // 4. Debug dump only if something changed
            if (anyChanges)
                debugToolStore();


            string newSpec = GetNewToolSchema();

            if (newSpec != _lastToolSpecJson)
            {
                Logger.Write("\n\nToolSpec: " + newSpec);
                _lastToolSpecJson = newSpec;
            }
            batch.NewTools.Clear();
            batch.UpdatedTools.Clear();
            batch.RemovedTools.Clear();

            if (anyChanges)
            {
                foreach (var kvp in LiveDllStore)
                {
                    var dllName = kvp.Key;
                    var entry = kvp.Value;

                    if (entry.Instance == null)
                        continue;

                    var inst = entry.Instance;
                    var type = inst.GetType();

                    string toolName = type.GetProperty("Name")?.GetValue(inst)?.ToString();

                    if (!string.IsNullOrWhiteSpace(toolName))
                    {
                        DllNameToToolName[dllName] = toolName;
                        ToolNameToDllName[toolName] = dllName;

                        Logger.Write($"Map: DLL '{dllName}' → Tool '{toolName}'");
                    }
                }
            }

        }

        /*        public void ReadDllFromDisk(string name)
                {
                    if (!LiveDllStore.ContainsKey(name))
                    {
                        LiveDllStore[name] = new LiveDllEntry
                        {
                            Name = name,
                            Timestamp = GetRealTimestamp(name),
                            Bytes = GetRealBytes(name),
                            flag = ChangeFlag.None,
                            Dirty = false
                        };
                    }
                }*/
        /*        public void ReadDllFromDisk(string name)
                {
                    if (!LiveDllStore.ContainsKey(name))
                    {
                        var bytes = GetRealBytes(name);
                        var ts = GetRealTimestamp(name);

                        // 1. Add to LiveDllStore (existing behaviour)
                        LiveDllStore[name] = new LiveDllEntry
                        {
                            Name = name,
                            Timestamp = ts,
                            Bytes = bytes,
                            flag = ChangeFlag.None,
                            Dirty = false
                        };

                        // 2. Add to DBDllStore (new behaviour)
                        var entry = new DllDbEntry
                        {
                            Name = name,
                            Bytes = bytes,
                            Timestamp = ts,
                            Description = "",

                            IsLive = true,
                            IsCore = false,          // you can override this later
                            ChangeFlag = "None",
                            Dirty = false,

                            LoadCount = 0,
                            LastLoaded = null
                        };

                        DBDllStore.Save(entry);
                    }
                    Logger.Write("\nDB: " + string.Join(", ", DBDllStore.ListNames()));
                }*/

        public void ReadDllFromDisk(string name)
        {
            if (!LiveDllStore.ContainsKey(name))
            {
                // 1. Get timestamp from disk
                var diskTs = GetRealTimestamp(name);

                byte[] bytes;

                // 2. Try DB first
                var db = DBDllStore.GetBytesAndTimestamp(name);

                if (db != null)
                {
                    var (dbBytes, dbTs) = db.Value;

                    if (dbTs == diskTs)
                    {
                        // MATCH → use DB bytes
                        Logger.Write($"Using cached DB bytes for {name}");
                        bytes = dbBytes;
                    }
                    else
                    {
                        // MISMATCH → read real bytes
                        Logger.Write($"Timestamp changed for {name}, reading real bytes");
                        bytes = GetRealBytes(name);
                    }
                }
                else
                {
                    // NOT IN DB → read real bytes
                    Logger.Write($"No DB entry for {name}, reading real bytes");
                    bytes = GetRealBytes(name);
                }

                // 3. Add to LiveDllStore
                LiveDllStore[name] = new LiveDllEntry
                {
                    Name = name,
                    Timestamp = diskTs,
                    Bytes = bytes,
                    flag = ChangeFlag.None,
                    Dirty = false
                };

                // 4. Save/update DB
                DBDllStore.Save(new DllDbEntry
                {
                    Name = name,
                    Bytes = bytes,
                    Timestamp = diskTs,
                    Description = "",
                    IsLive = true,
                    IsCore = false,
                    ChangeFlag = "None",
                    Dirty = false,
                    LoadCount = 0,
                    LastLoaded = null
                });
            }

            Logger.Write("\nDB: " + string.Join(", ", DBDllStore.ListNames()));
        }


        private DateTime GetRealTimestamp(string dllName)
        {
            var path = Path.Combine(ToolRefresher._folder, dllName+".dll");
            return File.GetLastWriteTime(path);
        }
        private byte[] GetRealBytes(string dllName)
        {
            Logger.Write($"\nReading real bytes for {dllName} from disk at {ToolRefresher._folder}");
            var path = Path.Combine(ToolRefresher._folder, dllName + ".dll");
            return File.ReadAllBytes(path);
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
        private void BuildDllChain(string name)
        {
            if (!LiveDllStore.TryGetValue(name, out var entry))
                throw new Exception($"Entry not found: {name}");

            // 1. Load assembly from bytes
            var alc = new AssemblyLoadContext(Guid.NewGuid().ToString(), isCollectible: true);
            using var ms = new MemoryStream(entry.Bytes);
            var asm = alc.LoadFromStream(ms);

            // 2. Find the tool type
            var toolType = asm.GetTypes()
                .FirstOrDefault(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.GetProperty("Name") != null &&
                    t.GetProperty("Description") != null &&
                    t.GetProperty("Schema") != null &&
                    t.GetProperty("Type") != null &&
                    t.GetProperty("CanUse") != null);


            if (toolType == null)
                throw new Exception($"No tool type found in {name}");

            // 3. Create instance
            var instance = Activator.CreateInstance(toolType);

            // 4. Store everything back into the entry
            entry.Alc = alc;
            entry.Assembly = asm;
            entry.Instance = instance;

            Logger.Write("\nBuilt DLL chain for " + name);
        }

        public string GetNewToolSchema()
        {
            var list = new List<object>();

            foreach (var entry in LiveDllStore.OrderBy(k => k.Key).Select(k => k.Value))

            {
                if (entry.Instance == null)
                    continue;

                var inst = entry.Instance;
                var type = inst.GetType();

                string name = type.GetProperty("Name")?.GetValue(inst)?.ToString();
                string description = type.GetProperty("Description")?.GetValue(inst)?.ToString();
                string schemaJson = type.GetProperty("Schema")?.GetValue(inst)?.ToString();
                string toolType = type.GetProperty("Type")?.GetValue(inst)?.ToString();
                string canUse = type.GetProperty("CanUse")?.GetValue(inst)?.ToString();

                JsonElement parameters;
                try
                {
                    parameters = JsonSerializer.Deserialize<JsonElement>(schemaJson);
                }
                catch
                {
                    parameters = JsonDocument.Parse("{}").RootElement;
                }

                list.Add(new
                {
                    name,
                    description,
                    parameters,
                    canUse,
                    type = toolType
                });
            }

            return JsonSerializer.Serialize(list); // compact one-line JSON
        }

        public ToolProbeResult ProbeToolMetadata(byte[] bytes)
            {
                var alc = new AssemblyLoadContext("Probe", isCollectible: true);

                try
                {
                    using var ms = new MemoryStream(bytes);
                    var asm = alc.LoadFromStream(ms);

                    // You can tighten this later (e.g. by interface or attribute)
                    var toolType = asm.GetTypes()
                        .FirstOrDefault(t =>
                            t.GetProperty("Name") != null &&
                            t.GetProperty("Description") != null &&
                            t.GetProperty("Schema") != null);

                    if (toolType == null)
                    {
                        return new ToolProbeResult
                        {
                            IsTool = false
                        };
                    }

                    var instance = Activator.CreateInstance(toolType);

                    string name = toolType.GetProperty("Name")?.GetValue(instance)?.ToString();
                    string description = toolType.GetProperty("Description")?.GetValue(instance)?.ToString();
                    string schema = toolType.GetProperty("Schema")?.GetValue(instance)?.ToString();
                    string type = toolType.GetProperty("Type")?.GetValue(instance)?.ToString();
                    string canUse = toolType.GetProperty("CanUse")?.GetValue(instance)?.ToString();

                    return new ToolProbeResult
                    {
                        IsTool = true,
                        Name = name,
                        Description = description,
                        Schema = schema,
                        Type = type,
                        CanUse = canUse
                    };
                }
                finally
                {
                    alc.Unload();
                }
    }



    public void debugToolStore()
        {
            Logger.Write($"\nLIVE DLLs: {LiveDllStore.Count}\n");
            foreach (var kvp in LiveDllStore)
            {
                Logger.Write($"LIVE - {kvp.Key} (Timestamp: {kvp.Value.Timestamp}) Flag: {kvp.Value.flag} Bytes held: {kvp.Value.Bytes.Length}\n");
            }
            Logger.Write($"\nRESERVE DLLs: {ReserveDllStore.Count} \n");
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