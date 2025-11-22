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
        services.AddTransient<MyBusinessLogic>();

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
                        return new BatchActivityExportProcessor(new ConduitTraceExporter(collector, "DirectoryService"));
                    });
            });
    }

    protected override void Configure(WebApplication app)
    {
        base.Configure(app);

        app.MapGet("/trigger", async (MyBusinessLogic logic) => 
        {
            await logic.DoWorkAsync();
            return Results.Ok("RPC Call Completed. Check Console/Logs.");
        });
    }
}

public class MyBusinessLogic
{
    private readonly IUserService _userService;

    public MyBusinessLogic(IUserService userService)
    {
        _userService = userService;
    }

    public async Task DoWorkAsync()
    {
        var user = new UserDto { Name = "DirectoryAdmin", Email = "admin@conduit.net", Username = "admin" };
        await _userService.RegisterUserAsync(user);
    }
}

public partial class Program { }

