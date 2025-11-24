using ConduitNet.Examples.AclService;
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
    public AclNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // AclService needs to call UserService to get user roles
        services.AddConduitClient<IUserService>();
        services.AddConduitClient<ITelemetryCollector>();
        
        RegisterConduitService<IAclService, AclService>();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AclService"))
                    .AddSource(ConduitTelemetry.Source.Name)
                    .AddConsoleExporter()
                    .AddProcessor(sp => 
                    {
                        var collector = sp.GetRequiredService<ITelemetryCollector>();
                        var nodeContext = sp.GetRequiredService<NodeContext>();
                        return new BatchActivityExportProcessor(new ConduitTraceExporter(collector, "AclService", nodeContext.NodeId));
                    });
            });
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IAclService>();
    }
}
