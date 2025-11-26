using System.Collections.Concurrent;
using FinanceSupplyChain.Contracts.Finance1;

namespace GeneralLedger.Node;

public class GeneralLedgerService : IGeneralLedgerService
{
    private readonly ConcurrentDictionary<Guid, AccountDto> _accounts = new();
    private readonly ConcurrentDictionary<Guid, JournalEntryDto> _journalEntries = new();
    
    public GeneralLedgerService()
    {
        // Seed some sample data
        SeedData();
    }

    private void SeedData()
    {
        var accounts = new[]
        {
            new AccountDto { Id = Guid.NewGuid(), AccountNumber = "1000", Name = "Cash", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit, Balance = 50000m, Currency = "USD", IsActive = true },
            new AccountDto { Id = Guid.NewGuid(), AccountNumber = "1100", Name = "Accounts Receivable", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit, Balance = 25000m, Currency = "USD", IsActive = true },
            new AccountDto { Id = Guid.NewGuid(), AccountNumber = "1500", Name = "Inventory", Type = AccountType.Asset, NormalBalance = NormalBalance.Debit, Balance = 75000m, Currency = "USD", IsActive = true },
            new AccountDto { Id = Guid.NewGuid(), AccountNumber = "2000", Name = "Accounts Payable", Type = AccountType.Liability, NormalBalance = NormalBalance.Credit, Balance = 15000m, Currency = "USD", IsActive = true },
            new AccountDto { Id = Guid.NewGuid(), AccountNumber = "3000", Name = "Retained Earnings", Type = AccountType.Equity, NormalBalance = NormalBalance.Credit, Balance = 100000m, Currency = "USD", IsActive = true },
            new AccountDto { Id = Guid.NewGuid(), AccountNumber = "4000", Name = "Sales Revenue", Type = AccountType.Revenue, NormalBalance = NormalBalance.Credit, Balance = 150000m, Currency = "USD", IsActive = true },
            new AccountDto { Id = Guid.NewGuid(), AccountNumber = "5000", Name = "Cost of Goods Sold", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, Balance = 80000m, Currency = "USD", IsActive = true },
            new AccountDto { Id = Guid.NewGuid(), AccountNumber = "6000", Name = "Operating Expenses", Type = AccountType.Expense, NormalBalance = NormalBalance.Debit, Balance = 35000m, Currency = "USD", IsActive = true },
        };
        
        foreach (var account in accounts)
        {
            _accounts[account.Id] = account;
        }
    }

    public Task<AccountDto[]> GetAccountsAsync()
    {
        return Task.FromResult(_accounts.Values.ToArray());
    }

    public Task<AccountDto?> GetAccountAsync(Guid accountId)
    {
        _accounts.TryGetValue(accountId, out var account);
        return Task.FromResult(account);
    }

    public Task<AccountDto?> GetAccountByNumberAsync(string accountNumber)
    {
        var account = _accounts.Values.FirstOrDefault(a => a.AccountNumber == accountNumber);
        return Task.FromResult(account);
    }

    public Task<JournalEntryDto[]> GetJournalEntriesAsync(DateTime from, DateTime to)
    {
        var entries = _journalEntries.Values
            .Where(e => e.EntryDate >= from && e.EntryDate <= to)
            .OrderByDescending(e => e.EntryDate)
            .ToArray();
        return Task.FromResult(entries);
    }

    public Task<JournalEntryDto?> GetJournalEntryAsync(Guid entryId)
    {
        _journalEntries.TryGetValue(entryId, out var entry);
        return Task.FromResult(entry);
    }

    public Task<JournalEntryDto> PostJournalEntryAsync(CreateJournalEntryRequest request)
    {
        // Validate debits = credits
        var totalDebits = request.Lines.Where(l => l.IsDebit).Sum(l => l.Amount);
        var totalCredits = request.Lines.Where(l => !l.IsDebit).Sum(l => l.Amount);
        
        if (totalDebits != totalCredits)
        {
            throw new InvalidOperationException("Journal entry must be balanced (debits must equal credits)");
        }

        var entryNumber = $"JE-{DateTime.Now:yyyyMMdd}-{_journalEntries.Count + 1:D4}";
        var lines = request.Lines.Select((l, i) => new JournalLineDto
        {
            LineNumber = i + 1,
            AccountId = l.AccountId,
            AccountNumber = _accounts.TryGetValue(l.AccountId, out var acc) ? acc.AccountNumber : "",
            AccountName = acc?.Name ?? "",
            Description = l.Description ?? "",
            DebitAmount = l.IsDebit ? l.Amount : 0,
            CreditAmount = l.IsDebit ? 0 : l.Amount
        }).ToArray();

        var entry = new JournalEntryDto
        {
            Id = Guid.NewGuid(),
            EntryNumber = entryNumber,
            EntryDate = request.EntryDate,
            Description = request.Description,
            Reference = request.Reference,
            Lines = lines,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            Status = JournalEntryStatus.Posted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        _journalEntries[entry.Id] = entry;

        // Update account balances
        foreach (var line in request.Lines)
        {
            if (_accounts.TryGetValue(line.AccountId, out var account))
            {
                var newBalance = account.NormalBalance == NormalBalance.Debit
                    ? account.Balance + (line.IsDebit ? line.Amount : -line.Amount)
                    : account.Balance + (line.IsDebit ? -line.Amount : line.Amount);
                
                _accounts[line.AccountId] = account with { Balance = newBalance };
            }
        }

        Console.WriteLine($"[GeneralLedger] Posted journal entry: {entryNumber}");
        return Task.FromResult(entry);
    }

    public Task<TrialBalanceDto> GetTrialBalanceAsync(DateTime asOfDate)
    {
        var accounts = _accounts.Values
            .Where(a => a.IsActive)
            .OrderBy(a => a.AccountNumber)
            .Select(a => new TrialBalanceLineDto
            {
                AccountId = a.Id,
                AccountNumber = a.AccountNumber,
                AccountName = a.Name,
                DebitBalance = a.NormalBalance == NormalBalance.Debit ? a.Balance : 0,
                CreditBalance = a.NormalBalance == NormalBalance.Credit ? a.Balance : 0
            })
            .ToArray();

        var trialBalance = new TrialBalanceDto
        {
            AsOfDate = asOfDate,
            GeneratedAt = DateTime.UtcNow,
            Accounts = accounts,
            TotalDebits = accounts.Sum(a => a.DebitBalance),
            TotalCredits = accounts.Sum(a => a.CreditBalance)
        };

        return Task.FromResult(trialBalance);
    }
}
