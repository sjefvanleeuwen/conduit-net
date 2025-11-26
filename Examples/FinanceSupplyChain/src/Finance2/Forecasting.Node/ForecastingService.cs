using FinanceSupplyChain.Contracts.Finance2;

namespace Forecasting.Node;

public class ForecastingService : IForecastingService
{
    public Task<CashFlowForecastDto> GetCashFlowForecastAsync(int horizonDays)
    {
        return GetCashFlowForecastAsync(DateTime.Today, DateTime.Today.AddDays(horizonDays));
    }

    public Task<CashFlowForecastDto> GetCashFlowForecastAsync(DateTime fromDate, DateTime toDate)
    {
        var periods = new List<CashFlowPeriodDto>();
        var currentBalance = 500000m; // Starting balance
        var minBalance = currentBalance;
        var minBalanceDate = fromDate;
        var totalInflows = 0m;
        var totalOutflows = 0m;
        var random = new Random(42); // Seeded for consistency

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            var openingBalance = currentBalance;
            
            // Generate mock inflows
            var inflowItems = new List<CashFlowItemDto>();
            if (date.Day == 15 || date.Day == 30)
            {
                inflowItems.Add(new CashFlowItemDto { Category = "Customer Receipts", Description = "AR Collections", Amount = 50000m + random.Next(-5000, 5000), Certainty = CashFlowCertainty.Expected });
            }
            if (date.DayOfWeek == DayOfWeek.Friday)
            {
                inflowItems.Add(new CashFlowItemDto { Category = "Sales", Description = "Weekly Sales Revenue", Amount = 15000m + random.Next(-2000, 2000), Certainty = CashFlowCertainty.Estimated });
            }

            // Generate mock outflows
            var outflowItems = new List<CashFlowItemDto>();
            if (date.Day == 1)
            {
                outflowItems.Add(new CashFlowItemDto { Category = "Rent", Description = "Monthly Office Rent", Amount = 25000m, Certainty = CashFlowCertainty.Confirmed });
            }
            if (date.Day == 15)
            {
                outflowItems.Add(new CashFlowItemDto { Category = "Payroll", Description = "Bi-weekly Payroll", Amount = 75000m, Certainty = CashFlowCertainty.Confirmed });
            }
            if (date.DayOfWeek == DayOfWeek.Wednesday)
            {
                outflowItems.Add(new CashFlowItemDto { Category = "AP Payments", Description = "Vendor Payments", Amount = 20000m + random.Next(-3000, 3000), Certainty = CashFlowCertainty.Expected });
            }

            var inflows = inflowItems.Sum(i => i.Amount);
            var outflows = outflowItems.Sum(o => o.Amount);
            totalInflows += inflows;
            totalOutflows += outflows;
            currentBalance = openingBalance + inflows - outflows;

            if (currentBalance < minBalance)
            {
                minBalance = currentBalance;
                minBalanceDate = date;
            }

            periods.Add(new CashFlowPeriodDto
            {
                Date = date,
                OpeningBalance = openingBalance,
                Inflows = inflows,
                Outflows = outflows,
                NetCashFlow = inflows - outflows,
                ClosingBalance = currentBalance,
                InflowItems = inflowItems.ToArray(),
                OutflowItems = outflowItems.ToArray()
            });
        }

        return Task.FromResult(new CashFlowForecastDto
        {
            Id = Guid.NewGuid(),
            GeneratedAt = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = 500000m,
            ClosingBalance = currentBalance,
            TotalInflows = totalInflows,
            TotalOutflows = totalOutflows,
            MinimumBalance = minBalance,
            MinimumBalanceDate = minBalanceDate,
            Periods = periods.ToArray()
        });
    }

    public Task<ForecastScenarioDto[]> GetScenariosAsync()
    {
        var scenarios = new[]
        {
            new ForecastScenarioDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Base Case", 
                Description = "Current business trajectory",
                Type = ScenarioType.Base,
                Assumptions = Array.Empty<ForecastAssumptionDto>()
            },
            new ForecastScenarioDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Optimistic", 
                Description = "20% increase in sales",
                Type = ScenarioType.Optimistic,
                Assumptions = new[] { new ForecastAssumptionDto { Parameter = "Sales", AdjustmentPercent = 20m, Notes = "Market expansion" } }
            },
            new ForecastScenarioDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Pessimistic", 
                Description = "15% decrease in sales, 10% increase in costs",
                Type = ScenarioType.Pessimistic,
                Assumptions = new[] 
                { 
                    new ForecastAssumptionDto { Parameter = "Sales", AdjustmentPercent = -15m, Notes = "Economic downturn" },
                    new ForecastAssumptionDto { Parameter = "Costs", AdjustmentPercent = 10m, Notes = "Inflation impact" }
                }
            }
        };
        return Task.FromResult(scenarios);
    }

    public async Task<CashFlowForecastDto> RunScenarioAsync(Guid scenarioId)
    {
        // For simplicity, return base forecast
        return await GetCashFlowForecastAsync(30);
    }

    public Task<ForecastAlertDto[]> GetForecastAlertsAsync()
    {
        var alerts = new[]
        {
            new ForecastAlertDto
            {
                Id = Guid.NewGuid(),
                Severity = AlertSeverity.Warning,
                Message = "Cash balance projected to fall below minimum threshold",
                AlertDate = DateTime.Today.AddDays(15),
                Amount = 50000m,
                Recommendation = "Consider delaying non-essential payments or accelerating collections"
            },
            new ForecastAlertDto
            {
                Id = Guid.NewGuid(),
                Severity = AlertSeverity.Info,
                Message = "Large payment due in 7 days",
                AlertDate = DateTime.Today.AddDays(7),
                Amount = 75000m,
                Recommendation = "Ensure sufficient funds in operating account"
            }
        };
        return Task.FromResult(alerts);
    }
}
