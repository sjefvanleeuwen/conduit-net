using System.Collections.Concurrent;
using FinanceSupplyChain.Contracts.Finance2;

namespace Treasury.Node;

public class TreasuryService : ITreasuryService
{
    private readonly ConcurrentDictionary<Guid, BankAccountDto> _accounts = new();
    private readonly ConcurrentDictionary<Guid, TransferDto> _transfers = new();
    private int _transferCounter = 3000;

    public TreasuryService()
    {
        SeedData();
    }

    private void SeedData()
    {
        var accounts = new[]
        {
            new BankAccountDto { Id = Guid.NewGuid(), AccountNumber = "****1234", AccountName = "Operating Account", BankName = "First National Bank", AccountType = BankAccountType.Checking, Currency = "USD", CurrentBalance = 500000m, AvailableBalance = 485000m, IsActive = true, LastReconciled = DateTime.Today.AddDays(-7) },
            new BankAccountDto { Id = Guid.NewGuid(), AccountNumber = "****5678", AccountName = "Payroll Account", BankName = "First National Bank", AccountType = BankAccountType.Checking, Currency = "USD", CurrentBalance = 150000m, AvailableBalance = 150000m, IsActive = true, LastReconciled = DateTime.Today.AddDays(-7) },
            new BankAccountDto { Id = Guid.NewGuid(), AccountNumber = "****9012", AccountName = "Reserve Fund", BankName = "Investment Bank Corp", AccountType = BankAccountType.MoneyMarket, Currency = "USD", CurrentBalance = 1000000m, AvailableBalance = 1000000m, IsActive = true, LastReconciled = DateTime.Today.AddDays(-30) },
            new BankAccountDto { Id = Guid.NewGuid(), AccountNumber = "****3456", AccountName = "EUR Operations", BankName = "European Bank", AccountType = BankAccountType.Checking, Currency = "EUR", CurrentBalance = 250000m, AvailableBalance = 245000m, IsActive = true, LastReconciled = DateTime.Today.AddDays(-14) },
        };

        foreach (var account in accounts)
        {
            _accounts[account.Id] = account;
        }
    }

    public Task<BankAccountDto[]> GetBankAccountsAsync()
    {
        return Task.FromResult(_accounts.Values.ToArray());
    }

    public Task<BankAccountDto?> GetBankAccountAsync(Guid accountId)
    {
        _accounts.TryGetValue(accountId, out var account);
        return Task.FromResult(account);
    }

    public Task<CashPositionDto> GetCashPositionAsync()
    {
        var byCurrency = _accounts.Values
            .GroupBy(a => a.Currency)
            .Select(g => new CurrencyPositionDto
            {
                Currency = g.Key,
                TotalBalance = g.Sum(a => a.CurrentBalance),
                AvailableBalance = g.Sum(a => a.AvailableBalance),
                AccountCount = g.Count()
            })
            .ToArray();

        var totalUSD = _accounts.Values
            .Sum(a => a.Currency == "USD" ? a.CurrentBalance : a.CurrentBalance * GetExchangeRate(a.Currency, "USD"));

        return Task.FromResult(new CashPositionDto
        {
            AsOfDate = DateTime.UtcNow,
            TotalCash = totalUSD,
            TotalAvailable = _accounts.Values.Sum(a => a.Currency == "USD" ? a.AvailableBalance : a.AvailableBalance * GetExchangeRate(a.Currency, "USD")),
            BaseCurrency = "USD",
            ByCurrency = byCurrency
        });
    }

    public Task<CashPositionDto> GetCashPositionAsync(string currency)
    {
        var accounts = _accounts.Values.Where(a => a.Currency == currency).ToArray();
        
        return Task.FromResult(new CashPositionDto
        {
            AsOfDate = DateTime.UtcNow,
            TotalCash = accounts.Sum(a => a.CurrentBalance),
            TotalAvailable = accounts.Sum(a => a.AvailableBalance),
            BaseCurrency = currency,
            ByCurrency = new[]
            {
                new CurrencyPositionDto
                {
                    Currency = currency,
                    TotalBalance = accounts.Sum(a => a.CurrentBalance),
                    AvailableBalance = accounts.Sum(a => a.AvailableBalance),
                    AccountCount = accounts.Length
                }
            }
        });
    }

    public Task<TransferDto> InitiateTransferAsync(CreateTransferRequest request)
    {
        if (!_accounts.TryGetValue(request.FromAccountId, out var fromAccount))
        {
            throw new InvalidOperationException("Source account not found");
        }

        if (!_accounts.TryGetValue(request.ToAccountId, out var toAccount))
        {
            throw new InvalidOperationException("Destination account not found");
        }

        if (fromAccount.AvailableBalance < request.Amount)
        {
            throw new InvalidOperationException("Insufficient funds");
        }

        var transfer = new TransferDto
        {
            Id = Guid.NewGuid(),
            TransferNumber = $"TRF-{++_transferCounter}",
            FromAccountId = fromAccount.Id,
            FromAccountName = fromAccount.AccountName,
            ToAccountId = toAccount.Id,
            ToAccountName = toAccount.AccountName,
            Amount = request.Amount,
            Currency = fromAccount.Currency,
            ExchangeRate = fromAccount.Currency != toAccount.Currency 
                ? GetExchangeRate(fromAccount.Currency, toAccount.Currency) 
                : null,
            ConvertedAmount = fromAccount.Currency != toAccount.Currency 
                ? request.Amount * GetExchangeRate(fromAccount.Currency, toAccount.Currency) 
                : null,
            Status = TransferStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            Reference = request.Reference,
            Notes = request.Notes
        };

        _transfers[transfer.Id] = transfer;
        Console.WriteLine($"[Treasury] Initiated transfer: {transfer.TransferNumber} - {transfer.Amount:N2} {transfer.Currency}");
        return Task.FromResult(transfer);
    }

    public Task<TransferDto> ApproveTransferAsync(Guid transferId, string approverNotes)
    {
        if (!_transfers.TryGetValue(transferId, out var transfer))
        {
            throw new InvalidOperationException("Transfer not found");
        }

        if (!_accounts.TryGetValue(transfer.FromAccountId, out var fromAccount) ||
            !_accounts.TryGetValue(transfer.ToAccountId, out var toAccount))
        {
            throw new InvalidOperationException("Account not found");
        }

        // Execute transfer
        _accounts[fromAccount.Id] = fromAccount with 
        { 
            CurrentBalance = fromAccount.CurrentBalance - transfer.Amount,
            AvailableBalance = fromAccount.AvailableBalance - transfer.Amount
        };

        var creditAmount = transfer.ConvertedAmount ?? transfer.Amount;
        _accounts[toAccount.Id] = toAccount with 
        { 
            CurrentBalance = toAccount.CurrentBalance + creditAmount,
            AvailableBalance = toAccount.AvailableBalance + creditAmount
        };

        var updated = transfer with 
        { 
            Status = TransferStatus.Completed,
            ApprovedAt = DateTime.UtcNow
        };
        _transfers[transferId] = updated;

        Console.WriteLine($"[Treasury] Approved transfer: {transfer.TransferNumber}");
        return Task.FromResult(updated);
    }

    public Task<TransferDto[]> GetTransfersAsync(DateTime from, DateTime to)
    {
        var transfers = _transfers.Values
            .Where(t => t.RequestedAt >= from && t.RequestedAt <= to)
            .OrderByDescending(t => t.RequestedAt)
            .ToArray();
        return Task.FromResult(transfers);
    }

    public Task<TransferDto[]> GetPendingTransfersAsync()
    {
        var pending = _transfers.Values
            .Where(t => t.Status == TransferStatus.Pending)
            .ToArray();
        return Task.FromResult(pending);
    }

    private decimal GetExchangeRate(string from, string to)
    {
        // Mock exchange rates
        if (from == "EUR" && to == "USD") return 1.08m;
        if (from == "USD" && to == "EUR") return 0.93m;
        if (from == "GBP" && to == "USD") return 1.27m;
        if (from == "USD" && to == "GBP") return 0.79m;
        return 1m;
    }
}
