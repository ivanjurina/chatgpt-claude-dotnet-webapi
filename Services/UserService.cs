using chatgpt_claude_dotnet_webapi.Contracts;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using chatgpt_claude_dotnet_webapi.Repositories;

namespace chatgpt_claude_dotnet_webapi.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsers();
        Task<UserDto> GetUserById(int id);
        Task<UserDto> CreateUser(CreateUserDto createUserDto);
        Task<bool> UpdateUser(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUser(int id);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto 
            { 
                Id = user.Id, 
                Username = user.Username, 
                Email = user.Email
            };
        }

        public async Task<IEnumerable<UserDto>> GetAllUsers()
        {
            var users = await _userRepository.GetAll();
            return users.Select(MapToDto);
        }

        public async Task<UserDto> GetUserById(int id)
        {
            var user = await _userRepository.GetById(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto> CreateUser(CreateUserDto createUserDto)
        {
            var user = new User 
            { 
                Username = createUserDto.Username, 
                Email = createUserDto.Email 
            };
            
            await _userRepository.Add(user);
            return MapToDto(user);
        }

        public async Task<bool> UpdateUser(int id, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetById(id);
            if (user == null) return false;

            user.Username = updateUserDto.Username;
            user.Email = updateUserDto.Email;

            await _userRepository.Update(user);
            return true;
        }

        public async Task<bool> DeleteUser(int id)
        {
            var user = await _userRepository.GetById(id);
            if (user == null) return false;

            await _userRepository.Delete(user);
            return true;
        }
    }
}