using ConduitNet.Node;
using ConduitNet.Server;
using FinanceSupplyChain.Contracts.Finance1;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

new AccountsReceivableNode(args).Run();

public class AccountsReceivableNode : ConduitNode
{
    public AccountsReceivableNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterConduitService<IAccountsReceivableService, AccountsReceivable.Node.AccountsReceivableService>();
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IAccountsReceivableService>();
    }
}

public partial class Program { }
