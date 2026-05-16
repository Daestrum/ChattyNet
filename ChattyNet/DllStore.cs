using System;
using System.Collections.Generic;
using System.IO;

namespace ChattyNet
{
    public class DLLStore
    {
        struct LiveDllEntry
        {
            public byte[] bytes;
            public DateTime timestamp;

            public object alc;      // later
            public object assembly; // later
            public object instance; // later
        }

        struct ReserveDllEntry
        {
            public byte[] bytes;
            public DateTime timestamp;
        }

        private int _maxLiveSize = 10; // or whatever you want


        Dictionary<string, LiveDllEntry> LiveDllStore = new Dictionary<string, LiveDllEntry>();
        Dictionary<string, ReserveDllEntry> ReserveDllStore = new Dictionary<string, ReserveDllEntry>();


        // -------------------------------
        // READ INTO STORE
        // -------------------------------
        public void Read(string name, string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            DateTime ts = File.GetLastWriteTime(path);

            if (LiveDllStore.Count < _maxLiveSize)
            {
                // Put into LIVE
                LiveDllEntry live = new LiveDllEntry
                {
                    bytes = bytes,
                    timestamp = ts
                };

                LiveDllStore[name] = live;
            }
            else
            {
                // Put into RESERVE
                ReserveDllEntry reserve = new ReserveDllEntry
                {
                    bytes = bytes,
                    timestamp = ts
                };

                ReserveDllStore[name] = reserve;
            }
        }



        // -------------------------------
        // GETTERS
        // -------------------------------
        public byte[] LiveGetBytes(string name)
        {
            return LiveDllStore[name].bytes;
        }

        public DateTime LiveGetTimestamp(string name)
        {
            return LiveDllStore[name].timestamp;
        }


        // -------------------------------
        // Demote = move LIVE → RESERVE
        // -------------------------------
        public void Demote(string name)
        {
            // 1. Get the live entry
            LiveDllEntry live = LiveDllStore[name];

            // 2. Create reserve entry
            ReserveDllEntry reserve = new ReserveDllEntry();
            reserve.bytes = live.bytes;
            reserve.timestamp = live.timestamp;

            // 3. Store in reserve
            ReserveDllStore[name] = reserve;

            // 4. Remove from live
            LiveDllStore.Remove(name);

            // 5. Later: unload ALC here
        }
        public void Promote(string name)
        {
            // 1. Get reserve entry
            ReserveDllEntry reserve = ReserveDllStore[name];

            // 2. Create live entry
            LiveDllEntry live = new LiveDllEntry
            {
                bytes = reserve.bytes,
                timestamp = reserve.timestamp
                // alc/assembly/instance added later
            };

            // 3. Store in live
            LiveDllStore[name] = live;

            // 4. Remove from reserve
            ReserveDllStore.Remove(name);

            // 5. Later: load ALC here
        }
        public void Swap(string name1, string name2)
        {
            bool name1IsLive = LiveDllStore.ContainsKey(name1);

            if (name1IsLive)
            {
                // name1 is LIVE, so name2 must be RESERVE
                Demote(name1);
                Promote(name2);
            }
            else
            {
                // name1 is RESERVE, so name2 must be LIVE
                Promote(name1);
                Demote(name2);
            }
        }

    }
}
