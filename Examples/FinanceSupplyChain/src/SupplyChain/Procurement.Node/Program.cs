using ConduitNet.Node;
using ConduitNet.Server;
using FinanceSupplyChain.Contracts.SupplyChain;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

new ProcurementNode(args).Run();

public class ProcurementNode : ConduitNode
{
    public ProcurementNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterConduitService<IProcurementService, Procurement.Node.ProcurementService>();
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IProcurementService>();
    }
}

public partial class Program { }
