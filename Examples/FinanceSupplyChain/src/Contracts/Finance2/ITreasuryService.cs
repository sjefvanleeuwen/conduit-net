using MessagePack;

namespace FinanceSupplyChain.Contracts.Finance2;

/// <summary>
/// Treasury service interface - Cash management and bank operations
/// </summary>
public interface ITreasuryService
{
    Task<BankAccountDto[]> GetBankAccountsAsync();
    Task<BankAccountDto> GetBankAccountAsync(Guid accountId);
    Task<CashPositionDto> GetCashPositionAsync();
    Task<CashPositionDto> GetCashPositionByCurrencyAsync(string currency);
    Task<BankTransactionDto[]> GetBankTransactionsAsync(Guid accountId, DateTime fromDate, DateTime toDate);
    Task ReconcileAccountAsync(Guid accountId, ReconciliationRequest request);
    Task<TransferDto> TransferFundsAsync(FundTransferRequest request);
}

[MessagePackObject]
public class BankAccountDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string AccountNumber { get; set; } = string.Empty;
    [Key(2)] public string AccountName { get; set; } = string.Empty;
    [Key(3)] public string BankCode { get; set; } = string.Empty;
    [Key(4)] public string BankName { get; set; } = string.Empty;
    [Key(5)] public string Currency { get; set; } = "USD";
    [Key(6)] public decimal CurrentBalance { get; set; }
    [Key(7)] public decimal AvailableBalance { get; set; }
    [Key(8)] public DateTime LastReconciled { get; set; }
    [Key(9)] public BankAccountType Type { get; set; }
    [Key(10)] public Guid GLAccountId { get; set; }
}

[MessagePackObject]
public class CashPositionDto
{
    [Key(0)] public DateTime AsOfDate { get; set; }
    [Key(1)] public CashPositionLineDto[] Positions { get; set; } = Array.Empty<CashPositionLineDto>();
    [Key(2)] public decimal TotalCash { get; set; }
    [Key(3)] public decimal TotalAvailable { get; set; }
}

[MessagePackObject]
public class CashPositionLineDto
{
    [Key(0)] public Guid BankAccountId { get; set; }
    [Key(1)] public string AccountName { get; set; } = string.Empty;
    [Key(2)] public string Currency { get; set; } = "USD";
    [Key(3)] public decimal Balance { get; set; }
    [Key(4)] public decimal Available { get; set; }
}

[MessagePackObject]
public class BankTransactionDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public Guid BankAccountId { get; set; }
    [Key(2)] public DateTime TransactionDate { get; set; }
    [Key(3)] public string Description { get; set; } = string.Empty;
    [Key(4)] public decimal Amount { get; set; }
    [Key(5)] public decimal RunningBalance { get; set; }
    [Key(6)] public BankTransactionType Type { get; set; }
    [Key(7)] public string? Reference { get; set; }
    [Key(8)] public bool IsReconciled { get; set; }
}

[MessagePackObject]
public class ReconciliationRequest
{
    [Key(0)] public DateTime StatementDate { get; set; }
    [Key(1)] public decimal StatementBalance { get; set; }
    [Key(2)] public Guid[] ReconciledTransactionIds { get; set; } = Array.Empty<Guid>();
}

[MessagePackObject]
public class TransferDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public Guid FromAccountId { get; set; }
    [Key(2)] public Guid ToAccountId { get; set; }
    [Key(3)] public decimal Amount { get; set; }
    [Key(4)] public DateTime TransferDate { get; set; }
    [Key(5)] public TransferStatus Status { get; set; }
    [Key(6)] public string? Reference { get; set; }
}

[MessagePackObject]
public class FundTransferRequest
{
    [Key(0)] public Guid FromAccountId { get; set; }
    [Key(1)] public Guid ToAccountId { get; set; }
    [Key(2)] public decimal Amount { get; set; }
    [Key(3)] public string? Reference { get; set; }
}

public enum BankAccountType
{
    Operating = 1,
    Savings = 2,
    MoneyMarket = 3,
    Investment = 4
}

public enum BankTransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Transfer = 3,
    Fee = 4,
    Interest = 5
}

public enum TransferStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}
