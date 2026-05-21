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
        public int IsLive { get; set; }
        public string Description { get; set; }
        public int ReturnCount { get; set; }
        public string ReturnLayout { get; set; }
        public string ToolName { get; set; }

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
                live INTEGER NOT NULL DEFAULT 0,    
                description TEXT,
                return_count INTEGER NOT NULL,
                return_layout TEXT NOT NULL,
                tool_name TEXT NOT NULL DEFAULT ''
                );
";
            cmd.ExecuteNonQuery();
        }

        public static void Save(DllDbEntry entry)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"INSERT OR REPLACE INTO dll_store 
              (name, bytes, timestamp, live, description, 
                return_count, return_layout,tool_name)
              VALUES 
              (@name, @bytes, @timestamp, @live, @description,
                @return_count, @return_layout, @tool_name);";

            cmd.Parameters.AddWithValue("@name", entry.Name);
            cmd.Parameters.AddWithValue("@bytes", entry.Bytes);
            cmd.Parameters.AddWithValue("@timestamp", entry.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("@live", entry.IsLive);
            cmd.Parameters.AddWithValue("@description", entry.Description);
            cmd.Parameters.AddWithValue("@return_count", entry.ReturnCount);
            cmd.Parameters.AddWithValue("@return_layout", entry.ReturnLayout);
            cmd.Parameters.AddWithValue("@tool_name", entry.ToolName);
            cmd.ExecuteNonQuery();
        }
        public static void InsertIfMissing(string name, byte[] bytes, DateTime timestamp)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"INSERT OR IGNORE INTO dll_store 
      (name, bytes, timestamp, live, description, return_count, return_layout)
      VALUES 
      (@name, @bytes, @timestamp, 0, '', 0, '{}');";

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@bytes", bytes);
            cmd.Parameters.AddWithValue("@timestamp", timestamp.ToString("o"));

            cmd.ExecuteNonQuery();
        }

        public static List<string> GetLiveTools()
        {
            var results = new List<string>();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT tool_name FROM dll_store WHERE live = 1;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }
        public static List<string> GetLiveDLLs()
        {
            var results = new List<string>();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM dll_store WHERE live = 1;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }
        public static List<string> GetReserveTools()
        {
            var results = new List<string>();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT tool_name FROM dll_store WHERE live = 0;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }

        public static string ByName(string toolName)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM dll_store WHERE tool_name = @toolName LIMIT 1;";
            cmd.Parameters.AddWithValue("@toolName", toolName);

            var result = cmd.ExecuteScalar();
            return result == null ? null : result.ToString();
        }

        public static DllDbEntry Get(string name)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"SELECT name, bytes, live, timestamp, description,
                return_count, return_layout
              FROM dll_store
              WHERE name = @name;";

            cmd.Parameters.AddWithValue("@name", name);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new DllDbEntry
            {
                Name = reader.GetString(reader.GetOrdinal("name")),
                Bytes = (byte[])reader["bytes"],
                Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("timestamp"))),
                IsLive = reader.GetInt32(reader.GetOrdinal("live")),
                Description = reader.IsDBNull(reader.GetOrdinal("description"))
                     ? ""
                     : reader.GetString(reader.GetOrdinal("description")),
                ReturnCount = reader.GetInt32(reader.GetOrdinal("return_count")),
                ReturnLayout = reader.GetString(reader.GetOrdinal("return_layout"))
            };
        }

        public static IEnumerable<DllDbEntry> ListAll()
        {
            var list = new List<DllDbEntry>();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"SELECT name, bytes, live, timestamp, description,
                return_count, return_layout 
              FROM dll_store;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DllDbEntry
                {
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Bytes = (byte[])reader["bytes"],
                    Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("timestamp"))),
                    IsLive = reader.GetInt32(reader.GetOrdinal("live")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                     ? ""
                     : reader.GetString(reader.GetOrdinal("description")),
                    ReturnCount = reader.GetInt32(reader.GetOrdinal("return_count")),
                    ReturnLayout = reader.GetString(reader.GetOrdinal("return_layout"))
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
            var ts = DateTime.Parse(reader.GetString(reader.GetOrdinal("timestamp")));

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
        public static void SetLiveStatus(string name, int islive)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dll_store SET live = @live WHERE name = @name;";
            cmd.Parameters.AddWithValue("@live", islive);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.ExecuteNonQuery();
        }
    }
}
