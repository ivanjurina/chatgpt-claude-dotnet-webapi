namespace chatgpt_claude_dotnet_webapi.Contracts
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? ChatId { get; set; }
    }
}