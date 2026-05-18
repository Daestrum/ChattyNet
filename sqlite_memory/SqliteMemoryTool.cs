using ChattyNet;
using ChattyNet.Shared;   // so it can see MemoryDB
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqliteMemoryTool
{
    public class SqliteMemoryTool
    {
        public string Name => "sqlite_memory";
        public string Description => @"
Execute SQL against the session-scoped in-memory SQLite database.
Use RAW SQL, but be aware that the database is shared.
This is beta test tool, so expect problems.";
        public string Schema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""action"": { 
        ""type"": ""string"",
        ""enum"": [""query"", ""non_query""]
    },
    ""sql"": { ""type"": ""string"" }
  },
  ""required"": [""action"", ""sql""]
}";

        public string Type => "action";
        public string CanUse => "free";
        public bool Tool => true;

        private class SqliteRequest
        {
            [JsonPropertyName("action")]
            public string Action { get; set; } = "";

            [JsonPropertyName("sql")]
            public string Sql { get; set; } = "";
        }

        private class SqliteResponse
        {
            [JsonPropertyName("kind")]
            public string Kind { get; set; } = "";

            [JsonPropertyName("rowsJson")]
            public string? RowsJson { get; set; }

            [JsonPropertyName("affected")]
            public int? Affected { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }

        public string Run(string jsonInput)
        {
            SqliteRequest req;

            try
            {
                req = JsonSerializer.Deserialize<SqliteRequest>(jsonInput)
                      ?? new SqliteRequest();
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new SqliteResponse
                {
                    Kind = "error",
                    Error = $"Invalid arguments: {ex.Message}"
                });
            }

            try
            {
                if (req.Action == "query")
                {
                    var rowsJson = MemoryDB.ExecuteQuery(req.Sql);
                    return JsonSerializer.Serialize(new SqliteResponse
                    {
                        Kind = "query",
                        RowsJson = rowsJson
                    });
                }

                if (req.Action == "non_query")
                {
                    var affected = MemoryDB.ExecuteNonQuery(req.Sql);
                    return JsonSerializer.Serialize(new SqliteResponse
                    {
                        Kind = "non_query",
                        Affected = affected
                    });
                }

                return JsonSerializer.Serialize(new SqliteResponse
                {
                    Kind = "error",
                    Error = $"Unknown action: {req.Action}"
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new SqliteResponse
                {
                    Kind = "error",
                    Error = ex.Message
                });
            }
        }
    }
}
