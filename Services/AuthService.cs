using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using chatgpt_claude_dotnet_webapi.Configuration;
using chatgpt_claude_dotnet_webapi.Contracts;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using chatgpt_claude_dotnet_webapi.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using chatgpt_claude_dotnet_webapi.Extensions;

namespace chatgpt_claude_dotnet_webapi.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> Login(LoginDto loginDto);
        Task<AuthResponseDto> Register(RegisterDto registerDto);
        Task<ServiceResponse<string>> ChangePassword(string newPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly JwtConfig _jwtConfig;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        public AuthService(
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _jwtConfig = _configuration.GetSection("JwtConfig").Get<JwtConfig>();
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponseDto> Login(LoginDto loginDto)
        {
            var user = await _userRepository.GetByUsername(loginDto.Username);
            if (user == null)
                throw new Exception("Invalid username or password");

            var result = user.PasswordHash == loginDto.Password;
            if (result != true)
                throw new Exception("Invalid username or password");

            var token = GenerateJwtToken(user);
            return new AuthResponseDto { Token = token, Username = user.Username };
        }

        public async Task<AuthResponseDto> Register(RegisterDto registerDto)
        {
            var existingUser = await _userRepository.GetByUsername(registerDto.Username);
            if (existingUser != null)
                throw new Exception("Username already exists");

            var existingEmail = await _userRepository.GetByEmail(registerDto.Email);
            if (existingEmail != null)
                throw new Exception("Email already exists");

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);
            await _userRepository.Add(user);

            var token = GenerateJwtToken(user);
            return new AuthResponseDto { Token = token, Username = user.Username };
        }

        public async Task<ServiceResponse<string>> ChangePassword(string newPassword)
        {
            var response = new ServiceResponse<string>();
            var user = await _userRepository.GetById(_httpContextAccessor.HttpContext.User.GetUserId());
            
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found.";
                return response;
            }

            user.PasswordHash = newPassword;
            await _userRepository.Update(user);

            response.Data = "Password successfully updated";
            return response;
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username)
                }),
                Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpiryInMinutes),
                Issuer = _jwtConfig.Issuer,
                Audience = _jwtConfig.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}