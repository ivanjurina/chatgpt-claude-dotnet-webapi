namespace chatgpt_claude_dotnet_webapi.Contracts
{
    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
    }
}