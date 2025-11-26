using MessagePack;

namespace FinanceSupplyChain.Contracts.Finance2;

/// <summary>
/// Risk Management service interface - Financial risk assessment and monitoring
/// </summary>
public interface IRiskManagementService
{
    Task<RiskExposureDto[]> GetRiskExposuresAsync();
    Task<RiskExposureDto> GetRiskExposureAsync(Guid exposureId);
    Task<CurrencyExposureDto[]> GetCurrencyExposuresAsync();
    Task<CounterpartyRiskDto[]> GetCounterpartyRisksAsync();
    Task<RiskLimitDto[]> GetRiskLimitsAsync();
    Task<RiskBreachDto[]> GetActiveBreachesAsync();
    Task<RiskReportDto> GenerateRiskReportAsync(DateTime asOfDate);
}

[MessagePackObject]
public class RiskExposureDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public RiskType RiskType { get; set; }
    [Key(2)] public string Description { get; set; } = string.Empty;
    [Key(3)] public decimal GrossExposure { get; set; }
    [Key(4)] public decimal NetExposure { get; set; }
    [Key(5)] public string Currency { get; set; } = "USD";
    [Key(6)] public decimal? VaR { get; set; }
    [Key(7)] public decimal? Probability { get; set; }
    [Key(8)] public string? Mitigation { get; set; }
    [Key(9)] public DateTime LastUpdated { get; set; }
}

[MessagePackObject]
public class CurrencyExposureDto
{
    [Key(0)] public string Currency { get; set; } = string.Empty;
    [Key(1)] public decimal Receivables { get; set; }
    [Key(2)] public decimal Payables { get; set; }
    [Key(3)] public decimal NetExposure { get; set; }
    [Key(4)] public decimal HedgedAmount { get; set; }
    [Key(5)] public decimal UnhedgedExposure { get; set; }
    [Key(6)] public decimal CurrentRate { get; set; }
    [Key(7)] public decimal? HedgeRate { get; set; }
}

[MessagePackObject]
public class CounterpartyRiskDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string CounterpartyName { get; set; } = string.Empty;
    [Key(2)] public string CounterpartyType { get; set; } = string.Empty;
    [Key(3)] public CreditRating Rating { get; set; }
    [Key(4)] public decimal TotalExposure { get; set; }
    [Key(5)] public decimal CreditLimit { get; set; }
    [Key(6)] public decimal UtilizationPercent { get; set; }
    [Key(7)] public string Currency { get; set; } = "USD";
    [Key(8)] public DateTime LastReviewDate { get; set; }
}

[MessagePackObject]
public class RiskLimitDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string LimitName { get; set; } = string.Empty;
    [Key(2)] public RiskType RiskType { get; set; }
    [Key(3)] public decimal LimitAmount { get; set; }
    [Key(4)] public decimal CurrentUsage { get; set; }
    [Key(5)] public decimal UtilizationPercent { get; set; }
    [Key(6)] public decimal WarningThreshold { get; set; }
    [Key(7)] public decimal CriticalThreshold { get; set; }
    [Key(8)] public LimitStatus Status { get; set; }
}

[MessagePackObject]
public class RiskBreachDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public Guid LimitId { get; set; }
    [Key(2)] public string LimitName { get; set; } = string.Empty;
    [Key(3)] public DateTime BreachDate { get; set; }
    [Key(4)] public decimal LimitAmount { get; set; }
    [Key(5)] public decimal ActualAmount { get; set; }
    [Key(6)] public decimal BreachPercent { get; set; }
    [Key(7)] public BreachSeverity Severity { get; set; }
    [Key(8)] public string? Resolution { get; set; }
    [Key(9)] public bool IsResolved { get; set; }
}

[MessagePackObject]
public class RiskReportDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public DateTime AsOfDate { get; set; }
    [Key(2)] public DateTime GeneratedAt { get; set; }
    [Key(3)] public decimal TotalVaR { get; set; }
    [Key(4)] public int ActiveBreaches { get; set; }
    [Key(5)] public int WarningLimits { get; set; }
    [Key(6)] public RiskExposureDto[] Exposures { get; set; } = Array.Empty<RiskExposureDto>();
    [Key(7)] public CurrencyExposureDto[] CurrencyExposures { get; set; } = Array.Empty<CurrencyExposureDto>();
    [Key(8)] public RiskLimitDto[] Limits { get; set; } = Array.Empty<RiskLimitDto>();
}

public enum RiskType
{
    Credit = 1,
    Market = 2,
    Currency = 3,
    Interest = 4,
    Liquidity = 5,
    Operational = 6
}

public enum CreditRating
{
    AAA = 1,
    AA = 2,
    A = 3,
    BBB = 4,
    BB = 5,
    B = 6,
    CCC = 7,
    Unrated = 8
}

public enum LimitStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3,
    Breached = 4
}

public enum BreachSeverity
{
    Minor = 1,
    Moderate = 2,
    Major = 3,
    Critical = 4
}
