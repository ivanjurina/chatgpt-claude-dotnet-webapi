using System.Net.Http.Headers;
using System.Text.Json;
using chatgpt_claude_dotnet_webapi.Configuration;

public interface ISpeechService
{
    IAsyncEnumerable<string> StreamSpeechToTextAsync(Stream audioStream);
}

public class SpeechService : ISpeechService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string WHISPER_API_URL = "https://api.openai.com/v1/audio/transcriptions";

    public SpeechService(ChatGptSettings settings, HttpClient httpClient)
    {
        _apiKey = settings.ApiKey;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async IAsyncEnumerable<string> StreamSpeechToTextAsync(Stream audioStream)
    {
        using var formData = new MultipartFormDataContent();
        using var streamContent = new StreamContent(audioStream);
        
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        formData.Add(streamContent, "file", "audio.mp3");
        formData.Add(new StringContent("whisper-1"), "model");

        var response = await _httpClient.PostAsync(WHISPER_API_URL, formData);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<WhisperResponse>();
        if (string.IsNullOrEmpty(result?.Text))
            yield break;

        // Split the text into words and stream them
        var words = result.Text.Split(' ');
        foreach (var word in words)
        {
            yield return word + " ";
            await Task.Delay(50); // Add a small delay between words
        }
    }

    private class WhisperResponse
    {
        public string Text { get; set; } = string.Empty;
    }
} 