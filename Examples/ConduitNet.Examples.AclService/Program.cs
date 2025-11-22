using ConduitNet.Examples.AclService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Contracts;
using ConduitNet.Server;
using ConduitNet.Node;
using ConduitNet.Client;

new AclNode(args).Run();

public class AclNode : ConduitNode
{
    public AclNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // AclService needs to call UserService to get user roles
        services.AddConduitClient<IUserService>();
        
        RegisterConduitService<IAclService, AclService>();
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IAclService>();
    }
}
