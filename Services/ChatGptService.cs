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

        var request = new HttpRequestMessage(HttpMethod.Post, API_URL);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        
        request.Content = JsonContent.Create(new
        {
            model = "gpt-3.5-turbo",
            messages = messages,
            stream = true
        });

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;
            if (line == "data: [DONE]") break;
            if (!line.StartsWith("data: ")) continue;

            var json = line.Substring(6);
            ChatGPTStreamResponse? chunk = null;
            
            try 
            {
                chunk = JsonSerializer.Deserialize<ChatGPTStreamResponse>(json);
            }
            catch (JsonException) 
            {
                continue; // Skip malformed JSON
            }

            var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
                await Task.Delay(10); // Small delay to control the stream rate
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