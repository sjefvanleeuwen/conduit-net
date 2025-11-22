using System.Threading.Tasks;
using ConduitNet.Contracts;
using ConduitNet.Core;

namespace Api2
{
    public class UserService : IUserService
    {
        public Task<UserDto> GetUserAsync(int id)
        {
            // Simulate DB call
            return Task.FromResult(new UserDto
            {
                Id = id,
                Name = "John Doe",
                Email = "john@example.com"
            });
        }

        [ConduitFilter(typeof(SimpleConsensusFilter))]
        public Task SaveUserAsync(UserDto user)
        {
            // Simulate save
            return Task.CompletedTask;
        }
    }
}

