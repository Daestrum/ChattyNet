using Chatty.Shared;
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
        public int return_count => 1;
        public string return_layout => "sqlite_response";

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
                return ToolUtils.WrapResult(1, "error", $"Invalid arguments: {ex.Message}");
            }

            try
            {
                if (req.Action == "query")
                {
                    var rowsJson = MemoryDB.ExecuteQuery(req.Sql);
                    return ToolUtils.WrapResult(return_count, return_layout, rowsJson);
                }

                if (req.Action == "non_query")
                {
                    var affected = MemoryDB.ExecuteNonQuery(req.Sql);
                    return ToolUtils.WrapResult(return_count, return_layout, affected.ToString());
                }

                return ToolUtils.WrapResult(1, "error", $"Unknown action: {req.Action}");
            }
            catch (Exception ex)
            {
                return ToolUtils.WrapResult(1, "error", ex.Message);
            }
        }
    }
}

