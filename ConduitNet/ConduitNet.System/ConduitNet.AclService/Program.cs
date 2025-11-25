using ConduitNet.AclService;
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

new AclNode(args).Run();

public class AclNode : ConduitNode
{
    public AclNode(string[] args) : base(args, "AclService") { }

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
