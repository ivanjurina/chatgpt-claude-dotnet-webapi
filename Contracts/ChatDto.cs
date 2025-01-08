namespace chatgpt_claude_dotnet_webapi.Contracts
{
    public class ChatHistoryDto
    {
        public int Id { get; set; }
        public string ChatId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
} 