using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChattyNet
{
    internal class LlmClient
    {
        private readonly HttpClient _http;
        private readonly string _url;

        public LlmClient(string baseUrl)
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(20)
            };
            _url = baseUrl.TrimEnd('/');
        }

        public async Task<JsonDocument> ChatAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            //Logger.Write($"[LLM SEND] {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{_url}/v1/chat/completions", content);

            var body = await response.Content.ReadAsStringAsync();

            //Logger.Write($"[LLM RAW RESPONSE] {body}");

            if (!response.IsSuccessStatusCode)
            {
                Logger.Write($"[LLM ERROR] {response.StatusCode} " + body);
                throw new Exception($"LLM returned HTTP {response.StatusCode}: {body}");
            }

            try
            {
                return JsonDocument.Parse(body);
            }
            catch (Exception ex)
            {
                Logger.Write($"[LLM JSON PARSE ERROR] {ex.Message}");
                throw new Exception($"Failed to parse LLM JSON:\n{body}", ex);
            }
        }


    }
}

