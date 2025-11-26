using MessagePack;

namespace FinanceSupplyChain.Contracts.Finance1;

/// <summary>
/// General Ledger service interface - Core accounting operations
/// </summary>
public interface IGeneralLedgerService
{
    Task<AccountDto[]> GetChartOfAccountsAsync();
    Task<AccountDto> GetAccountAsync(Guid accountId);
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request);
    Task<JournalEntryDto> PostJournalEntryAsync(PostJournalEntryRequest request);
    Task<JournalEntryDto[]> GetJournalEntriesAsync(DateTime fromDate, DateTime toDate);
    Task<TrialBalanceDto> GetTrialBalanceAsync(DateTime asOfDate);
    Task<decimal> GetAccountBalanceAsync(Guid accountId, DateTime asOfDate);
}

[MessagePackObject]
public class AccountDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string AccountNumber { get; set; } = string.Empty;
    [Key(2)] public string Name { get; set; } = string.Empty;
    [Key(3)] public AccountType Type { get; set; }
    [Key(4)] public bool IsActive { get; set; }
    [Key(5)] public decimal Balance { get; set; }
    [Key(6)] public Guid? ParentAccountId { get; set; }
}

[MessagePackObject]
public class CreateAccountRequest
{
    [Key(0)] public string AccountNumber { get; set; } = string.Empty;
    [Key(1)] public string Name { get; set; } = string.Empty;
    [Key(2)] public AccountType Type { get; set; }
    [Key(3)] public Guid? ParentAccountId { get; set; }
}

[MessagePackObject]
public class JournalEntryDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string EntryNumber { get; set; } = string.Empty;
    [Key(2)] public DateTime PostingDate { get; set; }
    [Key(3)] public string Description { get; set; } = string.Empty;
    [Key(4)] public JournalStatus Status { get; set; }
    [Key(5)] public JournalLineDto[] Lines { get; set; } = Array.Empty<JournalLineDto>();
    [Key(6)] public DateTime CreatedAt { get; set; }
}

[MessagePackObject]
public class JournalLineDto
{
    [Key(0)] public Guid AccountId { get; set; }
    [Key(1)] public string AccountNumber { get; set; } = string.Empty;
    [Key(2)] public decimal DebitAmount { get; set; }
    [Key(3)] public decimal CreditAmount { get; set; }
    [Key(4)] public string? CostCenter { get; set; }
}

[MessagePackObject]
public class PostJournalEntryRequest
{
    [Key(0)] public DateTime PostingDate { get; set; }
    [Key(1)] public string Description { get; set; } = string.Empty;
    [Key(2)] public JournalLineDto[] Lines { get; set; } = Array.Empty<JournalLineDto>();
}

[MessagePackObject]
public class TrialBalanceDto
{
    [Key(0)] public DateTime AsOfDate { get; set; }
    [Key(1)] public TrialBalanceLineDto[] Lines { get; set; } = Array.Empty<TrialBalanceLineDto>();
    [Key(2)] public decimal TotalDebits { get; set; }
    [Key(3)] public decimal TotalCredits { get; set; }
}

[MessagePackObject]
public class TrialBalanceLineDto
{
    [Key(0)] public Guid AccountId { get; set; }
    [Key(1)] public string AccountNumber { get; set; } = string.Empty;
    [Key(2)] public string AccountName { get; set; } = string.Empty;
    [Key(3)] public decimal Debit { get; set; }
    [Key(4)] public decimal Credit { get; set; }
}

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public enum JournalStatus
{
    Draft = 0,
    Posted = 1,
    Reversed = 2
}
