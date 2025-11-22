using MessagePack;
using System.Threading.Tasks;

namespace ConduitNet.Contracts
{
    public interface IUserService
    {
        Task<UserDto> GetUserAsync(int id);
        Task SaveUserAsync(UserDto user);
    }
}

