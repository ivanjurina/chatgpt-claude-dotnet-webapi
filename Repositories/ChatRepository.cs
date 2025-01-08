using Microsoft.EntityFrameworkCore;
using chatgpt_claude_dotnet_webapi.DataModel;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;

namespace chatgpt_claude_dotnet_webapi.Repositories;

public interface IChatRepository
{
    Task<Chat> GetOrCreateChatAsync(int userId, string? conversationId);
    Task<List<Message>> GetChatMessagesAsync(int chatId);
    Task SaveMessagesAsync(Message userMessage, Message assistantMessage);
}

public class ChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _context;

    public ChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Chat> GetOrCreateChatAsync(int userId, string? conversationId)
    {
        Chat? chat = null;
        if (!string.IsNullOrEmpty(conversationId))
        {
            chat = await _context.Chats
                .Include(x => x.Messages)
                .FirstOrDefaultAsync(c => 
                    c.UserId == userId && 
                    c.ConversationId == conversationId);
        }

        if (chat == null)
        {
            chat = new Chat
            {
                UserId = userId,
                ConversationId = conversationId ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
        }

        return chat;
    }

    public async Task<List<Message>> GetChatMessagesAsync(int chatId)
    {
        return await _context.Messages
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveMessagesAsync(Message userMessage, Message assistantMessage)
    {
        _context.Messages.Add(userMessage);
        _context.Messages.Add(assistantMessage);
        await _context.SaveChangesAsync();
    }
}