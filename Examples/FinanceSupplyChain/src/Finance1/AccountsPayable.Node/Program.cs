using ConduitNet.Node;
using ConduitNet.Server;
using FinanceSupplyChain.Contracts.Finance1;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

new AccountsPayableNode(args).Run();

public class AccountsPayableNode : ConduitNode
{
    public AccountsPayableNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterConduitService<IAccountsPayableService, AccountsPayable.Node.AccountsPayableService>();
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IAccountsPayableService>();
    }
}

public partial class Program { }
