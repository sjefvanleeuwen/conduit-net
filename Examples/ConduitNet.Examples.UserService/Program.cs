using ConduitNet.Examples.UserService;
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
    public UserNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterConduitService<IUserService, UserService>();
        
        // Register Telemetry Collector Client
        services.AddConduitClient<ITelemetryCollector>();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("UserService"))
                    .AddSource(ConduitTelemetry.Source.Name)
                    .AddConsoleExporter()
                    .AddProcessor(sp => 
                    {
                        var collector = sp.GetRequiredService<ITelemetryCollector>();
                        var nodeContext = sp.GetRequiredService<NodeContext>();
                        return new BatchActivityExportProcessor(new ConduitTraceExporter(collector, "UserService", nodeContext.NodeId));
                    });
            });
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IUserService>();
    }
}

public partial class Program { }

