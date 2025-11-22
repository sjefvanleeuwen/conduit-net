using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Contracts;
using ConduitNet.Server;

namespace ConduitNet.Node
{
    public class DirectoryNode : ConduitNode
    {
        public DirectoryNode(string[] args) : base(args) { }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConduitDirectory, ConduitDirectoryService>();
        }

        protected override void Configure(WebApplication app)
        {
            app.MapConduitService<IConduitDirectory>();
        }
    }
}
