using ConduitNet.Node;
using ConduitNet.Server;
using FinanceSupplyChain.Contracts.Finance2;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

new TreasuryNode(args).Run();

public class TreasuryNode : ConduitNode
{
    public TreasuryNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterConduitService<ITreasuryService, Treasury.Node.TreasuryService>();
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<ITreasuryService>();
    }
}

public partial class Program { }
