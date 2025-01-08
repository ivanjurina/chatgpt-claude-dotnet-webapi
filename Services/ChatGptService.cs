using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using chatgpt_claude_dotnet_webapi.Configuration;
using chatgpt_claude_dotnet_webapi.Contracts;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using chatgpt_claude_dotnet_webapi.Repositories;

namespace chatgpt_claude_dotnet_webapi.Services
{
    public interface IChatGptService
    {
        Task<ChatResponse> ChatAsync(int userId, ChatRequest request);
        Task<Chat> GetChatHistoryAsync(int userId, string conversationId);
    }

    public class ChatGptService : IChatGptService
    {
        private readonly HttpClient _httpClient;
        private readonly ChatGptSettings _settings;
        private readonly IChatRepository _repository; // Reusing the same repository
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";

        public ChatGptService(
            HttpClient httpClient,
            IOptions<ChatGptSettings> settings,
            IChatRepository repository)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _repository = repository;
            
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
        }

        public async Task<ChatResponse> ChatAsync(int userId, ChatRequest request)
        {
            var chat = await _repository.GetOrCreateChatAsync(userId, request.ConversationId);
            var chatHistory = await _repository.GetChatMessagesAsync(chat.Id);

            var messages = chatHistory.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToList();

            var requestBody = new
            {
                model = "gpt-4-turbo-preview",
                messages = messages.Concat(new[]
                {
                    new { role = "user", content = request.Message }
                }),
                max_tokens = _settings.MaxTokens,
                temperature = 0.7
            };

            using var httpContent = JsonContent.Create(requestBody);
            var response = await _httpClient.PostAsync(OPENAI_API_URL, httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"ChatGPT API error: {errorContent}");
            }
            
            var responseBody = await response.Content.ReadFromJsonAsync<ChatGptResponse>();
            var assistantMessage = responseBody?.Choices?.FirstOrDefault()?.Message?.Content 
                ?? throw new Exception("No response from ChatGPT");

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
                ConversationId = chat.ConversationId
            };
        }

        public async Task<Chat> GetChatHistoryAsync(int userId, string conversationId)
        {
            return await _repository.GetOrCreateChatAsync(userId, conversationId);
        }
    }

    public class ChatGptResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    public class Choice
    {
        public MessageGpt? Message { get; set; }
    }

    public class MessageGpt
    {
        public string? Content { get; set; }
    }
} 