using System;
using System.Linq;
using System.Threading.Tasks;
using ConduitNet.Core;
using ConduitNet.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ConduitNet.Client
{
    public class ServiceDiscoveryFilter : IConduitFilter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public ServiceDiscoveryFilter(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public async ValueTask<ConduitMessage> InvokeAsync(ConduitMessage message, ConduitDelegate next)
        {
            // 1. Check if we already have a target (e.g. from a previous redirect or manual set)
            if (message.Headers.ContainsKey("Target-Url"))
            {
                return await next(message);
            }

            // 2. Special handling for Directory calls to avoid recursion
            if (message.InterfaceName == nameof(IConduitDirectory))
            {
                var dirUrl = _configuration["Conduit:DirectoryUrl"] ?? "ws://localhost:5000/conduit";
                message.Headers["Target-Url"] = dirUrl;
                return await next(message);
            }

            // 3. Resolve the Directory Client
            var directory = _serviceProvider.GetService<IConduitDirectory>();

            if (directory == null)
            {
                // Fallback for when no directory is configured (e.g. unit tests or simple setup)
                // Default to localhost:5002 for demo purposes if not found
                message.Headers["Target-Url"] = "ws://localhost:5002/conduit";
                return await next(message);
            }

            // 4. Ask Directory for the service
            // We use the Interface Name as the Service Name
            var nodes = await directory.DiscoverAsync(message.InterfaceName);
            
            var targetNode = nodes.FirstOrDefault();

            if (targetNode == null)
            {
                throw new Exception($"ServiceDiscovery: No nodes found for service '{message.InterfaceName}'");
            }

            // 5. Set the Target URL
            // Assuming the Address in NodeInfo is the base URL (e.g., http://localhost:5002)
            // We need to convert it to the WebSocket endpoint
            var baseUri = new Uri(targetNode.Address);
            var scheme = baseUri.Scheme == "https" ? "wss" : "ws";
            var conduitUrl = $"{scheme}://{baseUri.Authority}/conduit";

            message.Headers["Target-Url"] = conduitUrl;

            return await next(message);
        }
    }
}

