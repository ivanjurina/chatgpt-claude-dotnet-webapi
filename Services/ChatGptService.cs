using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using chatgpt_claude_dotnet_webapi.Configuration;

public interface IChatGPTService
{
    Task<string> GetResponseAsync(string message, List<Message> history);
    IAsyncEnumerable<string> StreamResponseAsync(string message, List<Message> history);
}

public class ChatGPTService : IChatGPTService
{
    private readonly HttpClient _httpClient;
    private readonly ChatGptSettings _settings;
    private const string API_URL = "https://api.openai.com/v1/chat/completions";

    public ChatGPTService(ChatGptSettings settings, HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
    }

    public async Task<string> GetResponseAsync(string message, List<Message> history)
    {
        var messages = history.Select(m => new { role = m.Role, content = m.Content }).ToList();
        messages.Add(new { role = "user", content = message });

        var request = new
        {
            model = "gpt-3.5-turbo",
            messages = messages
        };

        var response = await _httpClient.PostAsJsonAsync(API_URL, request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatGPTResponse>();
        return result?.Choices?[0]?.Message?.Content ?? throw new Exception("No response from ChatGPT");
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(string message, List<Message> history)
    {
        var messages = history.Select(m => new { role = m.Role, content = m.Content }).ToList();
        messages.Add(new { role = "user", content = message });

        var request = new
        {
            model = "gpt-3.5-turbo",
            messages = messages,
            stream = true
        };

        var response = await _httpClient.PostAsJsonAsync(API_URL, request);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line) || line == "data: [DONE]") continue;
            if (!line.StartsWith("data: ")) continue;

            var json = line.Substring(6);
            var chunk = JsonSerializer.Deserialize<ChatGPTStreamResponse>(json);

            if (!string.IsNullOrEmpty(chunk?.Choices?[0]?.Delta?.Content))
            {
                yield return chunk.Choices[0].Delta.Content;
            }
        }
    }

    private class ChatGPTResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = new();

        public class Choice
        {
            [JsonPropertyName("message")]
            public ChatMessage Message { get; set; } = new();

            public class ChatMessage
            {
                [JsonPropertyName("content")]
                public string Content { get; set; } = string.Empty;
            }
        }
    }

    private class ChatGPTStreamResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = new();

        public class Choice
        {
            [JsonPropertyName("delta")]
            public DeltaContent Delta { get; set; } = new();

            public class DeltaContent
            {
                [JsonPropertyName("content")]
                public string Content { get; set; } = string.Empty;
            }
        }
    }
} 