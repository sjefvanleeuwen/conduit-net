using MessagePack;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConduitNet.Contracts
{
    public interface IAclService
    {
        Task CreateRoleAsync(string roleName);
        Task GrantPermissionAsync(string roleName, string permission);
        Task<bool> CheckPermissionAsync(int userId, string permission);
        Task<List<string>> GetRolePermissionsAsync(string roleName);
    }
}
