using ConduitNet.Node;
using ConduitNet.Server;
using FinanceSupplyChain.Contracts.Finance1;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

new GeneralLedgerNode(args).Run();

public class GeneralLedgerNode : ConduitNode
{
    public GeneralLedgerNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterConduitService<IGeneralLedgerService, GeneralLedger.Node.GeneralLedgerService>();
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IGeneralLedgerService>();
    }
}

public partial class Program { }
