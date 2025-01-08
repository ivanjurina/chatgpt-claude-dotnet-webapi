using System.Security.Claims;

namespace chatgpt_claude_dotnet_webapi.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) 
                ?? throw new InvalidOperationException("User ID not found in claims");
            return int.Parse(userId);
        }
    }
} 