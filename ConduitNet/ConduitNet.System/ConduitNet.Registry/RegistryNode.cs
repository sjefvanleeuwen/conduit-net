using ConduitNet.Contracts;
using ConduitNet.Node;
using ConduitNet.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ConduitNet.Registry
{
    public class RegistryNode : ConduitNode
    {
        public RegistryNode(string[] args) : base(args)
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            // Register the Registry Service implementation
            RegisterConduitService<IRegistryService, RegistryService>();
        }

        protected override void Configure(WebApplication app)
        {
            // Map the WebSocket middleware for RPC
            app.MapConduitService<IRegistryService>();
        }
    }
}
