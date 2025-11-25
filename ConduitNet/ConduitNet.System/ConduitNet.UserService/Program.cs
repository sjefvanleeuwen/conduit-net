using ConduitNet.UserService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Contracts;
using ConduitNet.Server;
using ConduitNet.Node;
using ConduitNet.Client;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using ConduitNet.Core;

new UserNode(args).Run();

public class UserNode : ConduitNode
{
    public UserNode(string[] args) : base(args, "UserService") { }

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

