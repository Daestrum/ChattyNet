using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;

namespace Chatty.Shared
{
    public class DllDbEntry
    {
        public string Name { get; set; }
        public byte[] Bytes { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }

        public bool IsLive { get; set; }
        public bool IsCore { get; set; }
        public string ChangeFlag { get; set; }
        public bool Dirty { get; set; }

        public int LoadCount { get; set; }
        public DateTime? LastLoaded { get; set; }
    }
    public static class DBDllStore
    {
        private static string _connectionString;

        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS dll_store (
                name TEXT PRIMARY KEY,
                bytes BLOB NOT NULL,
                timestamp TEXT NOT NULL,
                description TEXT,

                is_live INTEGER NOT NULL DEFAULT 0,
                is_core INTEGER NOT NULL DEFAULT 0,
                change_flag TEXT NOT NULL DEFAULT 'None',
                dirty INTEGER NOT NULL DEFAULT 0,

                load_count INTEGER NOT NULL DEFAULT 0,
                last_loaded TEXT
            );";
            cmd.ExecuteNonQuery();
        }

        public static void Save(DllDbEntry entry)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"INSERT OR REPLACE INTO dll_store 
              (name, bytes, timestamp, description, 
               is_live, is_core, change_flag, dirty, 
               load_count, last_loaded)
              VALUES 
              (@name, @bytes, @timestamp, @description,
               @is_live, @is_core, @change_flag, @dirty,
               @load_count, @last_loaded);";

            cmd.Parameters.AddWithValue("@name", entry.Name);
            cmd.Parameters.AddWithValue("@bytes", entry.Bytes);
            cmd.Parameters.AddWithValue("@timestamp", entry.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("@description", entry.Description);

            cmd.Parameters.AddWithValue("@is_live", entry.IsLive ? 1 : 0);
            cmd.Parameters.AddWithValue("@is_core", entry.IsCore ? 1 : 0);
            cmd.Parameters.AddWithValue("@change_flag", entry.ChangeFlag);
            cmd.Parameters.AddWithValue("@dirty", entry.Dirty ? 1 : 0);

            cmd.Parameters.AddWithValue("@load_count", entry.LoadCount);
            cmd.Parameters.AddWithValue("@last_loaded", entry.LastLoaded?.ToString("o"));

            cmd.ExecuteNonQuery();
        }

        public static bool Exists(string name)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM dll_store WHERE name = @name LIMIT 1;";
            cmd.Parameters.AddWithValue("@name", name);

            return cmd.ExecuteScalar() != null;
        }

        public static DllDbEntry Get(string name)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"SELECT name, bytes, timestamp, description,
                     is_live, is_core, change_flag, dirty,
                     load_count, last_loaded
              FROM dll_store
              WHERE name = @name;";

            cmd.Parameters.AddWithValue("@name", name);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new DllDbEntry
            {
                Name = reader.GetString(0),
                Bytes = (byte[])reader["bytes"],
                Timestamp = DateTime.Parse(reader.GetString(2)),
                Description = reader.IsDBNull(3) ? "" : reader.GetString(3),

                IsLive = reader.GetInt32(4) == 1,
                IsCore = reader.GetInt32(5) == 1,
                ChangeFlag = reader.GetString(6),
                Dirty = reader.GetInt32(7) == 1,

                LoadCount = reader.GetInt32(8),
                LastLoaded = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9))
            };
        }

        public static IEnumerable<DllDbEntry> ListAll()
        {
            var list = new List<DllDbEntry>();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"SELECT name, bytes, timestamp, description,
                     is_live, is_core, change_flag, dirty,
                     load_count, last_loaded
              FROM dll_store;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DllDbEntry
                {
                    Name = reader.GetString(0),
                    Bytes = (byte[])reader["bytes"],
                    Timestamp = DateTime.Parse(reader.GetString(2)),
                    Description = reader.IsDBNull(3) ? "" : reader.GetString(3),

                    IsLive = reader.GetInt32(4) == 1,
                    IsCore = reader.GetInt32(5) == 1,
                    ChangeFlag = reader.GetString(6),
                    Dirty = reader.GetInt32(7) == 1,

                    LoadCount = reader.GetInt32(8),
                    LastLoaded = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9))
                });
            }

            return list;
        }
        public static IEnumerable<string> ListNames()
        {
            const string sql = "SELECT Name FROM dll_store ORDER BY Name";

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                yield return reader.GetString(0);
        }
        public static (byte[] bytes, DateTime timestamp)? GetBytesAndTimestamp(string name)
        {
            const string sql = "SELECT bytes, timestamp FROM dll_store WHERE name = @name";

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", name);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            var bytes = (byte[])reader["bytes"];
            var ts = DateTime.Parse(reader.GetString(1));

            return (bytes, ts);
        }

        public static void Delete(string name)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM dll_store WHERE name = @name;";
            cmd.Parameters.AddWithValue("@name", name);

            cmd.ExecuteNonQuery();
        }
    }
}
