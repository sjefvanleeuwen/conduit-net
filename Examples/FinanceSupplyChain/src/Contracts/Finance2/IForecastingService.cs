using MessagePack;

namespace FinanceSupplyChain.Contracts.Finance2;

/// <summary>
/// Forecasting service interface - Cash flow projections and planning
/// </summary>
public interface IForecastingService
{
    Task<CashFlowForecastDto> GetCashFlowForecastAsync(int horizonDays);
    Task<CashFlowForecastDto> GetCashFlowForecastAsync(DateTime fromDate, DateTime toDate);
    Task<ForecastScenarioDto[]> GetScenariosAsync();
    Task<CashFlowForecastDto> RunScenarioAsync(Guid scenarioId);
    Task<ForecastAlertDto[]> GetForecastAlertsAsync();
}

[MessagePackObject]
public class CashFlowForecastDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public DateTime GeneratedAt { get; set; }
    [Key(2)] public DateTime FromDate { get; set; }
    [Key(3)] public DateTime ToDate { get; set; }
    [Key(4)] public decimal OpeningBalance { get; set; }
    [Key(5)] public decimal ClosingBalance { get; set; }
    [Key(6)] public decimal TotalInflows { get; set; }
    [Key(7)] public decimal TotalOutflows { get; set; }
    [Key(8)] public decimal MinimumBalance { get; set; }
    [Key(9)] public DateTime MinimumBalanceDate { get; set; }
    [Key(10)] public CashFlowPeriodDto[] Periods { get; set; } = Array.Empty<CashFlowPeriodDto>();
}

[MessagePackObject]
public class CashFlowPeriodDto
{
    [Key(0)] public DateTime Date { get; set; }
    [Key(1)] public decimal OpeningBalance { get; set; }
    [Key(2)] public decimal Inflows { get; set; }
    [Key(3)] public decimal Outflows { get; set; }
    [Key(4)] public decimal NetCashFlow { get; set; }
    [Key(5)] public decimal ClosingBalance { get; set; }
    [Key(6)] public CashFlowItemDto[] InflowItems { get; set; } = Array.Empty<CashFlowItemDto>();
    [Key(7)] public CashFlowItemDto[] OutflowItems { get; set; } = Array.Empty<CashFlowItemDto>();
}

[MessagePackObject]
public class CashFlowItemDto
{
    [Key(0)] public string Category { get; set; } = string.Empty;
    [Key(1)] public string Description { get; set; } = string.Empty;
    [Key(2)] public decimal Amount { get; set; }
    [Key(3)] public CashFlowCertainty Certainty { get; set; }
    [Key(4)] public string? SourceDocument { get; set; }
}

[MessagePackObject]
public class ForecastScenarioDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string Name { get; set; } = string.Empty;
    [Key(2)] public string Description { get; set; } = string.Empty;
    [Key(3)] public ScenarioType Type { get; set; }
    [Key(4)] public ForecastAssumptionDto[] Assumptions { get; set; } = Array.Empty<ForecastAssumptionDto>();
}

[MessagePackObject]
public class ForecastAssumptionDto
{
    [Key(0)] public string Parameter { get; set; } = string.Empty;
    [Key(1)] public decimal AdjustmentPercent { get; set; }
    [Key(2)] public string? Notes { get; set; }
}

[MessagePackObject]
public class ForecastAlertDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public AlertSeverity Severity { get; set; }
    [Key(2)] public string Message { get; set; } = string.Empty;
    [Key(3)] public DateTime AlertDate { get; set; }
    [Key(4)] public decimal? Amount { get; set; }
    [Key(5)] public string? Recommendation { get; set; }
}

public enum CashFlowCertainty
{
    Confirmed = 1,
    Expected = 2,
    Estimated = 3
}

public enum ScenarioType
{
    Base = 1,
    Optimistic = 2,
    Pessimistic = 3,
    Custom = 4
}

public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}
