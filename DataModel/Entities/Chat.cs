namespace chatgpt_claude_dotnet_webapi.DataModel.Entities;

public class Chat
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public string ConversationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public virtual List<Message> Messages { get; set; } = new List<Message>();
} 