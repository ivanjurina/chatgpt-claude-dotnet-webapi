using Microsoft.EntityFrameworkCore;
using chatgpt_claude_dotnet_webapi.DataModel;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;

namespace chatgpt_claude_dotnet_webapi.Repositories;

public interface IChatRepository
{
    Task<Chat> GetOrCreateChatAsync(int userId, int? chatId);
    Task<List<Message>> GetChatMessagesAsync(int chatId);
    Task SaveMessagesAsync(Message userMessage, Message assistantMessage);
    Task<IEnumerable<Chat>> GetUserChatsAsync(int userId);
}

public class ChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _context;

    public ChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Chat> GetOrCreateChatAsync(int userId, int? chatId)
    {
        Chat? chat = null;
        if (chatId.HasValue)
        {
            chat = await _context.Chats
                .Include(x => x.Messages)
                .FirstOrDefaultAsync(c => 
                    c.UserId == userId && 
                    c.Id == chatId);
        }

        if (chat == null)
        {
            chat = new Chat
            {
                UserId = userId,
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
        userMessage.Role = "user";
        assistantMessage.Role = "assistant";
        
        // If this is the first message in the chat, use it as the chat title
        var chat = await _context.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == userMessage.ChatId);

        if (chat != null && !chat.Messages.Any())
        {
            chat.Title = userMessage.Content.Length > 100 
                ? userMessage.Content.Substring(0, 97) + "..."
                : userMessage.Content;
        }

        _context.Messages.Add(userMessage);
        _context.Messages.Add(assistantMessage);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Chat>> GetUserChatsAsync(int userId)
    {
        return await _context.Chats
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}