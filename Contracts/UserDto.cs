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
        public bool IsActive { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        
        public bool IsActive { get; set; }
        
        public bool IsAdmin { get; set; }
    }
}