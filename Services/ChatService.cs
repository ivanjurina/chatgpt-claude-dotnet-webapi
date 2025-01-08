using chatgpt_claude_dotnet_webapi.Contracts;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using chatgpt_claude_dotnet_webapi.Repositories;

namespace chatgpt_claude_dotnet_webapi.Services;

public interface IChatService
{
    Task<ChatResponse> ChatAsync(int userId, ChatRequest request, string provider = "chatgpt");
    Task<Chat> GetChatHistoryAsync(int userId, int chatId);
    Task<IEnumerable<Chat>> GetUserChatsAsync(int userId);
}

public class ChatService : IChatService
{
    private readonly IClaudeService _claudeService;
    private readonly IChatGptService _chatGptService;
    private readonly IChatRepository _repository;
    public ChatService(
        IClaudeService claudeService,
        IChatGptService chatGptService,
        IChatRepository repository)
    {
        _claudeService = claudeService;
        _chatGptService = chatGptService;
        _repository = repository;
    }

    public async Task<ChatResponse> ChatAsync(int userId, ChatRequest request, string provider = "chatgpt")
    {
        return provider.ToLower() switch
        {
            "claude" => await _claudeService.ChatAsync(userId, request),
            "chatgpt" => await _chatGptService.ChatAsync(userId, request),
            _ => throw new ArgumentException($"Unsupported AI provider: {provider}")
        };
    }

    public async Task<Chat> GetChatHistoryAsync(int userId, int chatId)
    {
        return await _repository.GetOrCreateChatAsync(userId, chatId);
    }

    public async Task<IEnumerable<Chat>> GetUserChatsAsync(int userId)
    {
        return await _repository.GetUserChatsAsync(userId);
    }
}
