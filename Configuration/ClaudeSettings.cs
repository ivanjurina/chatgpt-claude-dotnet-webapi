namespace chatgpt_claude_dotnet_webapi.Configuration
{
    public class ClaudeSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "claude-3-sonnet-20240229";
        public int MaxTokens { get; set; } = 1024;
        public string DefaultSystem { get; set; } = "You are Claude, a helpful AI assistant.";
    }
}