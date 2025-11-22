using System.Threading.Tasks;
using ConduitNet.Contracts;

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

        public Task SaveUserAsync(UserDto user)
        {
            // Simulate save
            return Task.CompletedTask;
        }
    }
}
