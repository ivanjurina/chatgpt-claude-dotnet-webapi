using chatgpt_claude_dotnet_webapi.Contracts;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using chatgpt_claude_dotnet_webapi.Repositories;
using System;
using System.Text;
using System.Threading;

namespace chatgpt_claude_dotnet_webapi.Services;

public interface IChatService
{
    Task<ChatResponse> ChatAsync(int userId, ChatRequest request, string provider = "chatgpt");
    IAsyncEnumerable<ChatResponse> StreamChatAsync(int userId, ChatRequest request, string provider = "chatgpt");
    Task<Chat> GetChatHistoryAsync(int userId, int chatId);
    Task<PaginatedResult<Chat>> GetUserChatsAsync(
        int userId, 
        int pageNumber = 1, 
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<ChatResponse> ChatWithDocumentAsync(int userId, ChatRequest request, int documentId);
    IAsyncEnumerable<ChatResponse> StreamChatWithDocumentAsync(int userId, ChatRequest request, int documentId);
}

public class ChatService : IChatService
{
    private readonly IChatRepository _repository;
    private readonly IChatGPTService _chatGPTService;
    private readonly IDocumentService _documentService;

    public ChatService(
        IChatRepository repository, 
        IChatGPTService chatGPTService,
        IDocumentService documentService)
    {
        _repository = repository;
        _chatGPTService = chatGPTService;
        _documentService = documentService;
    }

    public async Task<ChatResponse> ChatAsync(int userId, ChatRequest request, string provider)
    {
        var chat = await _repository.GetOrCreateChatAsync(userId, request.ChatId);
        var chatHistory = await _repository.GetChatMessagesAsync(chat.Id);
        
        var response = await _chatGPTService.GetResponseAsync(request.Message, chatHistory);

        var userMessage = new Message
        {
            ChatId = chat.Id,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow,
            Role = "user"
        };

        var assistantMessage = new Message
        {
            ChatId = chat.Id,
            Content = response,
            CreatedAt = DateTime.UtcNow,
            Role = "bot"
        };

        await _repository.SaveMessagesAsync(userMessage, assistantMessage);

        return new ChatResponse
        {
            Message = response,
            ChatId = chat.Id
        };
    }

    public async IAsyncEnumerable<ChatResponse> StreamChatAsync(int userId, ChatRequest request, string provider)
    {
        var chat = await _repository.GetOrCreateChatAsync(userId, request.ChatId);
        var chatHistory = await _repository.GetChatMessagesAsync(chat.Id);
        
        var userMessage = new Message
        {
            ChatId = chat.Id,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow,
            Role = "user"
        };

        var assistantMessage = new Message
        {
            ChatId = chat.Id,
            Content = string.Empty,
            CreatedAt = DateTime.UtcNow,
            Role = "bot"
        };

        var fullResponse = new StringBuilder();
        await foreach (var chunk in _chatGPTService.StreamResponseAsync(request.Message, chatHistory))
        {
            fullResponse.Append(chunk);
            yield return new ChatResponse 
            { 
                Message = chunk,
                ChatId = chat.Id,
                IsComplete = false
            };
        }

        assistantMessage.Content = fullResponse.ToString();
        await _repository.SaveMessagesAsync(userMessage, assistantMessage);

        yield return new ChatResponse 
        { 
            Message = fullResponse.ToString(),
            ChatId = chat.Id,
            IsComplete = true
        };
    }

    public async Task<Chat> GetChatHistoryAsync(int userId, int chatId)
    {
        var chat = await _repository.GetOrCreateChatAsync(userId, chatId);
        return chat;
    }

    public async Task<PaginatedResult<Chat>> GetUserChatsAsync(
        int userId, 
        int pageNumber = 1, 
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetUserChatsAsync(userId, pageNumber, pageSize, cancellationToken);
    }

    public async Task<ChatResponse> ChatWithDocumentAsync(int userId, ChatRequest request, int documentId)
    {
        var chat = await _repository.GetOrCreateChatAsync(userId, request.ChatId);
        var chatHistory = await _repository.GetChatMessagesAsync(chat.Id);
        
        // Get document content
        var document = await _documentService.GetDocumentAsync(userId, documentId);
        
        // Create a system message with document context
        var systemMessage = new Message
        {
            Role = "system",
            Content = $"Use the following document content as context for answering: {document.ExtractedText}",
            CreatedAt = DateTime.UtcNow
        };

        // Add system message to history
        var historyWithContext = new List<Message> { systemMessage };
        historyWithContext.AddRange(chatHistory);

        var response = await _chatGPTService.GetResponseAsync(request.Message, historyWithContext);

        var userMessage = new Message
        {
            ChatId = chat.Id,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow,
            Role = "user"
        };

        var assistantMessage = new Message
        {
            ChatId = chat.Id,
            Content = response,
            CreatedAt = DateTime.UtcNow,
            Role = "bot"
        };

        await _repository.SaveMessagesAsync(userMessage, assistantMessage);

        return new ChatResponse
        {
            Message = response,
            ChatId = chat.Id
        };
    }

    public async IAsyncEnumerable<ChatResponse> StreamChatWithDocumentAsync(
        int userId, 
        ChatRequest request, 
        int documentId)
    {
        var chat = await _repository.GetOrCreateChatAsync(userId, request.ChatId);
        var chatHistory = await _repository.GetChatMessagesAsync(chat.Id);
        
        // Get document content
        var document = await _documentService.GetDocumentAsync(userId, documentId);
        
        // Create a system message with document context
        var systemMessage = new Message
        {
            Role = "system",
            Content = $"Use the following document content as context for answering: {document.ExtractedText}",
            CreatedAt = DateTime.UtcNow
        };

        // Add system message to history
        var historyWithContext = new List<Message> { systemMessage };
        historyWithContext.AddRange(chatHistory);

        var userMessage = new Message
        {
            ChatId = chat.Id,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow,
            Role = "user"
        };

        var assistantMessage = new Message
        {
            ChatId = chat.Id,
            Content = string.Empty,
            CreatedAt = DateTime.UtcNow,
            Role = "bot"
        };

        var fullResponse = new StringBuilder();
        await foreach (var chunk in _chatGPTService.StreamResponseAsync(request.Message, historyWithContext))
        {
            fullResponse.Append(chunk);
            yield return new ChatResponse 
            { 
                Message = chunk,
                ChatId = chat.Id,
                IsComplete = false
            };
        }

        assistantMessage.Content = fullResponse.ToString();
        await _repository.SaveMessagesAsync(userMessage, assistantMessage);

        yield return new ChatResponse 
        { 
            Message = fullResponse.ToString(),
            ChatId = chat.Id,
            IsComplete = true
        };
    }
}
