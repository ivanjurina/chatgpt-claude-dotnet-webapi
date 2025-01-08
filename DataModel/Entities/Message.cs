namespace chatgpt_claude_dotnet_webapi.DataModel.Entities;

public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public required string Content { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedAt { get; set; }
} 