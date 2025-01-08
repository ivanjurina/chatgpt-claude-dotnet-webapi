namespace chatgpt_claude_dotnet_webapi.DataModel.Entities;

public class Chat
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Message> Messages { get; set; } = new();
    public User User { get; set; } = null!;
} 