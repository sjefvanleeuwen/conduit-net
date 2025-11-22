using ConduitNet.Examples.UserService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Contracts;
using ConduitNet.Server;
using ConduitNet.Node;

new UserNode(args).Run();

public class UserNode : ConduitNode
{
    public UserNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterConduitService<IUserService, UserService>();
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IUserService>();
    }
}

public partial class Program { }

