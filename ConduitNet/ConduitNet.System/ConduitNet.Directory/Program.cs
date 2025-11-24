using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Contracts;
using ConduitNet.Client;
using ConduitNet.Node;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using ConduitNet.Core;

new MyDirectoryNode(args).Run();

public class MyDirectoryNode : DirectoryNode
{
    public MyDirectoryNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        
        // Register Client for IUserService (so we can test calling Api2)
        services.AddConduitClient<IUserService>();
        services.AddConduitClient<ITelemetryCollector>();
        
        // Run a background task to simulate traffic
        services.AddHostedService<WorkloadGenerator>();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("DirectoryService"))
                    .AddSource(ConduitTelemetry.Source.Name)
                    .AddConsoleExporter()
                    .AddProcessor(sp => {
                        var collector = sp.GetRequiredService<ITelemetryCollector>();
                        var nodeContext = sp.GetRequiredService<NodeContext>();
                        return new BatchActivityExportProcessor(new ConduitTraceExporter(collector, "DirectoryService", nodeContext.NodeId));
                    });
            });
    }

    protected override void Configure(WebApplication app)
    {
        base.Configure(app);
    }
}

public class WorkloadGenerator : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public WorkloadGenerator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the mesh to stabilize
        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try 
            {
                using var scope = _serviceProvider.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                
                var user = new UserDto { Name = "DirectoryAdmin", Email = "admin@conduit.net", Username = "admin" };
                await userService.RegisterUserAsync(user);
                
                Console.WriteLine("[WorkloadGenerator] Registered user via RPC");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorkloadGenerator] RPC failed: {ex.Message}");
            }

            await Task.Delay(10000, stoppingToken);
        }
    }
}

public partial class Program { }

