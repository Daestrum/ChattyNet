using System;
using System.IO;
using System.Collections.Generic;

public class DLLStore
{
    class LiveDllEntry
    {
        public byte[] Bytes { get; set; }
        public DateTime Timestamp { get; set; }
        public object Alc { get; set; }
        public object Assembly { get; set; }
        public object Instance { get; set; }
    }
    class ReserveDllEntry
    {
        public byte[] Bytes { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private int _maxLiveSize = 10;
    Dictionary<string, LiveDllEntry> LiveDllStore = new();
    Dictionary<string, ReserveDllEntry> ReserveDllStore = new();
    public void Read(string name, string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        DateTime ts = File.GetLastWriteTime(path);
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
    public byte[] LiveGetBytes(string name) => LiveDllStore[name].Bytes;
    public DateTime LiveGetTimestamp(string name) => LiveDllStore[name].Timestamp;
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
