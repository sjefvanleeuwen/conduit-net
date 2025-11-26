using ConduitNet.Node;
using ConduitNet.Server;
using FinanceSupplyChain.Contracts.SupplyChain;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

new InventoryNode(args).Run();

public class InventoryNode : ConduitNode
{
    public InventoryNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterConduitService<IInventoryService, Inventory.Node.InventoryService>();
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IInventoryService>();
    }
}

public partial class Program { }
