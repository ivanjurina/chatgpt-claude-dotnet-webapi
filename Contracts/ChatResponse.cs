namespace chatgpt_claude_dotnet_webapi.Contracts
{
    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public int ChatId { get; set; }
        public bool IsComplete { get; set; }
    }
}