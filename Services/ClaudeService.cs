using System.Net.Http.Json;
using System.Text.Json;
using chatgpt_claude_dotnet_webapi.Configuration;
using Microsoft.Extensions.Options;
using chatgpt_claude_dotnet_webapi.Contracts;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using chatgpt_claude_dotnet_webapi.Repositories;

namespace chatgpt_claude_dotnet_webapi.Services
{
    public interface IClaudeService
    {
        Task<ChatResponse> ChatAsync(int userId, ChatRequest request);
    }
    
    public class ClaudeService : IClaudeService
    {
        private readonly HttpClient _httpClient;
        private readonly ClaudeSettings _settings;
        private readonly IChatRepository _repository;
        private const string ANTHROPIC_API_URL = "https://api.anthropic.com/v1/messages";

        public ClaudeService(
            HttpClient httpClient,
            IOptions<ClaudeSettings> settings,
            IChatRepository repository)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _repository = repository;
            
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<ChatResponse> ChatAsync(int userId, ChatRequest request)
        {
            var chat = await _repository.GetOrCreateChatAsync(userId, request.ChatId);
            var chatHistory = await _repository.GetChatMessagesAsync(chat.Id);

            var requestBody = new
            {
                model = "claude-3-sonnet-20240229",
                max_tokens = _settings.MaxTokens,
                messages = new[]
                {
                    new { role = "user", content = request.Message }
                }
            };

            using var httpContent = JsonContent.Create(requestBody);
            var response = await _httpClient.PostAsync(ANTHROPIC_API_URL, httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Claude API error: {errorContent}");
            }
            
            var responseBody = await response.Content.ReadFromJsonAsync<ClaudeResponse>();
            var assistantMessage = responseBody?.Content?.FirstOrDefault()?.Text 
                ?? throw new Exception("No response from Claude");

            // Save user message
            var userMessage = new Message
            {
                ChatId = chat.Id,
                Role = "user",
                Content = request.Message,
                CreatedAt = DateTime.UtcNow
            };

            // Save assistant message
            var assistantDbMessage = new Message
            {
                ChatId = chat.Id,
                Role = "assistant",
                Content = assistantMessage,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.SaveMessagesAsync(userMessage, assistantDbMessage);

            return new ChatResponse
            {
                Message = assistantMessage,
                ChatId = chat.Id
            };
        }
    }

    // Helper class for deserializing Claude's response
    public class ClaudeResponse
    {
        public List<ContentItem>? Content { get; set; }
    }

    public class ContentItem
    {
        public string? Text { get; set; }
    }
}