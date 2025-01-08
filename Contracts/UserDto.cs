using System.ComponentModel.DataAnnotations;

namespace chatgpt_claude_dotnet_webapi.Contracts
{
    public class CreateUserDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateUserDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Email { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}