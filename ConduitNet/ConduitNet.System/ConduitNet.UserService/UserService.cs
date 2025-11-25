using System.Collections.Concurrent;
using ConduitNet.Contracts;
using ConduitNet.Core;
using ConduitNet.Node;

namespace ConduitNet.UserService
{
    public class UserService : IUserService
    {
        private static readonly ConcurrentDictionary<int, UserDto> _users = new();
        private static int _idCounter = 1;

        public Task<UserDto?> GetUserAsync(int id)
        {
            if (_users.TryGetValue(id, out var user))
            {
                return Task.FromResult<UserDto?>(user);
            }
            return Task.FromResult<UserDto?>(null);
        }

        public Task<List<UserDto>> GetAllUsersAsync()
        {
            return Task.FromResult(_users.Values.ToList());
        }

        [ConduitFilter(typeof(SimpleConsensusFilter))]
        public Task<UserDto> RegisterUserAsync(UserDto user)
        {
            var newUser = user with { Id = Interlocked.Increment(ref _idCounter) };
            _users[newUser.Id] = newUser;
            Console.WriteLine($"[UserService] Registered user: {newUser.Username} ({newUser.Id})");
            return Task.FromResult(newUser);
        }

        [ConduitFilter(typeof(SimpleConsensusFilter))]
        public Task UpdateUserAsync(UserDto user)
        {
            if (_users.ContainsKey(user.Id))
            {
                _users[user.Id] = user;
                Console.WriteLine($"[UserService] Updated user: {user.Id}");
            }
            return Task.CompletedTask;
        }

        [ConduitFilter(typeof(SimpleConsensusFilter))]
        public Task DeleteUserAsync(int id)
        {
            _users.TryRemove(id, out _);
            Console.WriteLine($"[UserService] Deleted user: {id}");
            return Task.CompletedTask;
        }
    }
}

