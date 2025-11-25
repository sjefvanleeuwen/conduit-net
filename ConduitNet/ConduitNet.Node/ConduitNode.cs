using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Server;
using ConduitNet.Client;
using System.Collections.Generic;
using ConduitNet.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ConduitNet.Node
{
    public class ConduitNodeRegistrationService : IHostedService
    {
        private readonly IConduitDirectory _directory;
        private readonly IConfiguration _config;
        private readonly List<string> _services;
        private readonly NodeContext _nodeContext;

        public ConduitNodeRegistrationService(IConduitDirectory directory, IConfiguration config, List<string> services, NodeContext nodeContext)
        {
            _directory = directory;
            _config = config;
            _services = services;
            _nodeContext = nodeContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Don't register if we are the directory itself (optional check, but good for safety)
            // Or if we have no services to offer.
            if (_services.Count == 0) return;

            string? address = _config["Conduit:NodeUrl"];
            
            if (string.IsNullOrEmpty(address))
            {
                 int port = _config.GetValue<int>("Conduit:Port", 5000);
                 // Default to WSS if certs are present (we assume they are if we are here)
                 // But we need to know if we are secure.
                 // For now, let's assume WSS if not specified.
                 address = $"wss://localhost:{port}";
            }

            // Clean up address if it has multiple (e.g. "ws://localhost:5000;wss://localhost:5001")
            address = address!.Split(';')[0];

            var nodeId = _nodeContext.NodeId;

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
            
            // Suppress the "Now listening on: http://..." message which confuses users expecting "ws://"
            Builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

            // Find Certificates
            var certPath = FindCertPath();
            var nodeCertPath = Path.Combine(certPath, "node.pfx");
            var caCertPath = Path.Combine(certPath, "ca.crt");
            var hasCerts = File.Exists(nodeCertPath) && File.Exists(caCertPath);

            var port = Builder.Configuration.GetValue<int?>("Conduit:Port");

            if (port.HasValue)
            {
                Builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(port.Value, listenOptions =>
                    {
                        if (hasCerts)
                        {
                            var nodeCert = new X509Certificate2(nodeCertPath, "conduit");
                            var caCert = new X509Certificate2(caCertPath);

                            listenOptions.UseHttps(httpsOptions =>
                            {
                                httpsOptions.ServerCertificate = nodeCert;
                                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                                httpsOptions.ClientCertificateValidation = (cert, chain, errors) =>
                                {
                                    if (cert == null) return false;
                                    // Simple check: Issuer must be our CA
                                    return cert.Issuer == caCert.Subject;
                                };
                            });
                            Console.WriteLine($"[ConduitNode] Secure Mode (mTLS) Enabled on port {port.Value}");
                        }
                        else
                        {
                            Console.WriteLine($"[ConduitNode] WARNING: Certificates not found at {certPath}. Running in INSECURE HTTP mode on port {port.Value}");
                        }
                    });
                });
            }

            ConfigureCoreServices(Builder.Services);
        }

        private string FindCertPath()
        {
            // Look in current dir, then up a few levels (for dev)
            var current = Directory.GetCurrentDirectory();
            for (int i = 0; i < 5; i++)
            {
                var path = Path.Combine(current, "certs");
                if (Directory.Exists(path)) return path;
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }
            return "certs"; // Default
        }

        private void ConfigureCoreServices(IServiceCollection services)
        {
            // Determine Node ID once
            var nodeId = Builder.Configuration["Conduit:NodeId"] ?? Guid.NewGuid().ToString();
            services.AddSingleton(new NodeContext { NodeId = nodeId });

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
