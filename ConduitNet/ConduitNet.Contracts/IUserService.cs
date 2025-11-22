using MessagePack;
using System.Threading.Tasks;

namespace ConduitNet.Contracts
{
    public interface IUserService
    {
        Task<UserDto?> GetUserAsync(int id);
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto> RegisterUserAsync(UserDto user);
        Task UpdateUserAsync(UserDto user);
        Task DeleteUserAsync(int id);
    }
}

