using System;
using System.Data;
using System.Data.SQLite;
using System.Text.Json;
namespace ChattyNet.Shared
{
    public static class MemoryDB
    {
        private static readonly SQLiteConnection _conn;

        static MemoryDB()
        {
            _conn = new SQLiteConnection("Data Source=:memory:;Version=3;");
            _conn.Open();
        }

        public static string ExecuteQuery(string sql)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();

            var rows = new List<Dictionary<string, object>>();

            while (reader.Read())
            {
                var row = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);

                    // Convert DBNull to null
                    if (value is DBNull)
                        value = null;

                    row[reader.GetName(i)] = value!;
                }

                rows.Add(row);
            }

            return JsonSerializer.Serialize(rows);
        }


        public static int ExecuteNonQuery(string sql)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }
    }
}