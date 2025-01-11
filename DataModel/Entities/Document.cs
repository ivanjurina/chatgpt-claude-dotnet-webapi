using chatgpt_claude_dotnet_webapi.DataModel.Entities;

namespace chatgpt_claude_dotnet_webapi.DataModel.Entities;

public class Document
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? ChatId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Chat? Chat { get; set; }
} 