using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Contracts;
using ConduitNet.Server;
using ConduitNet.Node;

namespace ConduitNet.Directory
{
    public class DirectoryNode : ConduitNode
    {
        public DirectoryNode(string[] args) : base(args, "DirectoryService") { }

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
