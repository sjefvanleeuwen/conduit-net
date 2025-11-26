using ConduitNet.Node;
using ConduitNet.Server;
using FinanceSupplyChain.Contracts.Finance2;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

new ForecastingNode(args).Run();

public class ForecastingNode : ConduitNode
{
    public ForecastingNode(string[] args) : base(args) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RegisterConduitService<IForecastingService, Forecasting.Node.ForecastingService>();
    }

    protected override void Configure(WebApplication app)
    {
        app.MapConduitService<IForecastingService>();
    }
}

public partial class Program { }
