namespace chatgpt_claude_dotnet_webapi.Configuration
{
    public class ChatGptSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 1024;
    }
} 