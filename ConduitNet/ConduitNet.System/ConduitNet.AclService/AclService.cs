using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ConduitNet.Contracts;
using ConduitNet.Core;
using ConduitNet.Client;
using ConduitNet.Node;

namespace ConduitNet.AclService
{
    public class AclService : IAclService
    {
        // Role -> Permissions
        private static readonly ConcurrentDictionary<string, HashSet<string>> _rolePermissions = new();
        private readonly IUserService _userService;

        public AclService(IUserService userService)
        {
            _userService = userService;
            
            // Seed some data
            _rolePermissions.TryAdd("Admin", new HashSet<string> { "CreateUser", "DeleteUser", "ViewUser" });
            _rolePermissions.TryAdd("User", new HashSet<string> { "ViewUser" });
        }

        [ConduitFilter(typeof(SimpleConsensusFilter))]
        public Task CreateRoleAsync(string roleName)
        {
            _rolePermissions.TryAdd(roleName, new HashSet<string>());
            Console.WriteLine($"[AclService] Created role: {roleName}");
            return Task.CompletedTask;
        }

        [ConduitFilter(typeof(SimpleConsensusFilter))]
        public Task GrantPermissionAsync(string roleName, string permission)
        {
            if (_rolePermissions.TryGetValue(roleName, out var perms))
            {
                lock (perms)
                {
                    perms.Add(permission);
                }
                Console.WriteLine($"[AclService] Granted {permission} to {roleName}");
            }
            return Task.CompletedTask;
        }

        public Task<List<string>> GetRolePermissionsAsync(string roleName)
        {
            if (_rolePermissions.TryGetValue(roleName, out var perms))
            {
                lock (perms)
                {
                    return Task.FromResult(perms.ToList());
                }
            }
            return Task.FromResult(new List<string>());
        }

        public async Task<bool> CheckPermissionAsync(int userId, string permission)
        {
            // 1. Get User
            var user = await _userService.GetUserAsync(userId);
            if (user == null) return false;

            // 2. Check Roles
            foreach (var role in user.Roles)
            {
                if (_rolePermissions.TryGetValue(role, out var perms))
                {
                    lock (perms)
                    {
                        if (perms.Contains(permission))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
