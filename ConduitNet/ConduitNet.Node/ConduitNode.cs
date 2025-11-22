using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Server;
using ConduitNet.Client;
using System.Collections.Generic;
using ConduitNet.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConduitNet.Node
{
    public class ConduitNodeRegistrationService : IHostedService
    {
        private readonly IConduitDirectory _directory;
        private readonly IConfiguration _config;
        private readonly List<string> _services;

        public ConduitNodeRegistrationService(IConduitDirectory directory, IConfiguration config, List<string> services)
        {
            _directory = directory;
            _config = config;
            _services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Don't register if we are the directory itself (optional check, but good for safety)
            // Or if we have no services to offer.
            if (_services.Count == 0) return;

            var address = _config["Conduit:NodeUrl"] ?? _config["urls"] ?? "http://localhost:5000";
            // Clean up address if it has multiple (e.g. "http://localhost:5000;https://localhost:5001")
            address = address.Split(';')[0];

            var nodeId = _config["Conduit:NodeId"] ?? Guid.NewGuid().ToString();

            try 
            {
                // We might need a retry policy here because the Directory might not be up yet
                // For now, we just try once. In a real system, use Polly.
                await _directory.RegisterAsync(new NodeInfo 
                { 
                    Id = nodeId,
                    Address = address,
                    Services = _services
                });
                Console.WriteLine($"[ConduitNode] Registered {nodeId} at {address} with services: {string.Join(", ", _services)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConduitNode] Failed to register with Directory: {ex.Message}");
                // Non-fatal, maybe directory is down.
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public abstract class ConduitNode
    {
        protected WebApplicationBuilder Builder { get; }
        protected WebApplication? App { get; private set; }
        private readonly List<string> _providedServices = new();

        protected ConduitNode(string[] args)
        {
            Builder = WebApplication.CreateBuilder(args);
            
            // Default Node Configuration
            ConfigureCoreServices(Builder.Services);
        }

        private void ConfigureCoreServices(IServiceCollection services)
        {
            // Every node is a Server (can accept RPC)
            services.AddConduitServer();

            // Every node is a Client (can route RPC)
            services.AddConduitLeaderRouting();

            // Every node needs to talk to the Directory
            services.AddConduitClient<IConduitDirectory>();

            // Register the list of provided services so the background service can access it
            services.AddSingleton(_providedServices);
            
            // Register the background service that performs the registration
            services.AddHostedService<ConduitNodeRegistrationService>();
        }

        protected void RegisterConduitService<TInterface, TImplementation>() 
            where TInterface : class 
            where TImplementation : class, TInterface
        {
            Builder.Services.AddSingleton<TInterface, TImplementation>();
            _providedServices.Add(typeof(TInterface).Name);
        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Override to add application specific services
        }

        protected virtual void Configure(WebApplication app)
        {
            // Override to map endpoints
        }

        public void Run()
        {
            ConfigureServices(Builder.Services);
            
            App = Builder.Build();
            
            App.UseWebSockets();
            
            Configure(App);

            App.Run();
        }
    }
}
