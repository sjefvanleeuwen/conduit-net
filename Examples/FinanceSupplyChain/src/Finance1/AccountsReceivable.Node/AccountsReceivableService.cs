using System.Collections.Concurrent;
using FinanceSupplyChain.Contracts.Finance1;

namespace AccountsReceivable.Node;

public class AccountsReceivableService : IAccountsReceivableService
{
    private readonly ConcurrentDictionary<Guid, CustomerDto> _customers = new();
    private readonly ConcurrentDictionary<Guid, CustomerInvoiceDto> _invoices = new();
    private readonly ConcurrentDictionary<Guid, ReceiptDto> _receipts = new();
    private int _invoiceCounter = 2000;
    private int _receiptCounter = 2000;

    public AccountsReceivableService()
    {
        SeedData();
    }

    private void SeedData()
    {
        var customers = new[]
        {
            new CustomerDto { Id = Guid.NewGuid(), CustomerNumber = "C001", Name = "Acme Corporation", ContactName = "Alice Johnson", Email = "alice@acme.com", Phone = "555-1001", BillingAddress = "100 Corporate Way, Business City, ST 10001", CreditLimit = 100000m, PaymentTerms = "Net 30", IsActive = true },
            new CustomerDto { Id = Guid.NewGuid(), CustomerNumber = "C002", Name = "Global Industries", ContactName = "Bob Martinez", Email = "bob@global.com", Phone = "555-1002", BillingAddress = "200 Industry Rd, Metro City, ST 10002", CreditLimit = 250000m, PaymentTerms = "Net 45", IsActive = true },
            new CustomerDto { Id = Guid.NewGuid(), CustomerNumber = "C003", Name = "Local Retail Shop", ContactName = "Carol White", Email = "carol@localretail.com", Phone = "555-1003", BillingAddress = "300 Main Street, Town, ST 10003", CreditLimit = 25000m, PaymentTerms = "Net 15", IsActive = true },
        };

        foreach (var customer in customers)
        {
            _customers[customer.Id] = customer;
        }

        // Create sample invoices
        var c1 = customers[0];
        var invoice1 = new CustomerInvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{++_invoiceCounter}",
            CustomerId = c1.Id,
            CustomerName = c1.Name,
            InvoiceDate = DateTime.Today.AddDays(-15),
            DueDate = DateTime.Today.AddDays(15),
            Status = CustomerInvoiceStatus.Sent,
            Lines = new[]
            {
                new CustomerInvoiceLineDto { LineNumber = 1, Description = "Consulting Services - Project A", Quantity = 40, UnitPrice = 150.00m, LineTotal = 6000.00m },
                new CustomerInvoiceLineDto { LineNumber = 2, Description = "Software License - Annual", Quantity = 1, UnitPrice = 5000.00m, LineTotal = 5000.00m }
            },
            Subtotal = 11000.00m,
            Tax = 880.00m,
            Total = 11880.00m,
            AmountPaid = 0,
            Balance = 11880.00m,
            Currency = "USD"
        };
        _invoices[invoice1.Id] = invoice1;

        var c2 = customers[1];
        var invoice2 = new CustomerInvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{++_invoiceCounter}",
            CustomerId = c2.Id,
            CustomerName = c2.Name,
            InvoiceDate = DateTime.Today.AddDays(-45),
            DueDate = DateTime.Today.AddDays(-15),
            Status = CustomerInvoiceStatus.Overdue,
            Lines = new[]
            {
                new CustomerInvoiceLineDto { LineNumber = 1, Description = "Equipment Sale", Quantity = 5, UnitPrice = 2500.00m, LineTotal = 12500.00m }
            },
            Subtotal = 12500.00m,
            Tax = 1000.00m,
            Total = 13500.00m,
            AmountPaid = 5000.00m,
            Balance = 8500.00m,
            Currency = "USD"
        };
        _invoices[invoice2.Id] = invoice2;
    }

    public Task<CustomerDto[]> GetCustomersAsync()
    {
        return Task.FromResult(_customers.Values.ToArray());
    }

    public Task<CustomerDto?> GetCustomerAsync(Guid customerId)
    {
        _customers.TryGetValue(customerId, out var customer);
        return Task.FromResult(customer);
    }

    public Task<CustomerInvoiceDto[]> GetInvoicesAsync()
    {
        return Task.FromResult(_invoices.Values.OrderByDescending(i => i.InvoiceDate).ToArray());
    }

    public Task<CustomerInvoiceDto[]> GetInvoicesByCustomerAsync(Guid customerId)
    {
        var invoices = _invoices.Values.Where(i => i.CustomerId == customerId).ToArray();
        return Task.FromResult(invoices);
    }

    public Task<CustomerInvoiceDto[]> GetOutstandingInvoicesAsync()
    {
        var outstanding = _invoices.Values
            .Where(i => i.Status != CustomerInvoiceStatus.Paid && i.Status != CustomerInvoiceStatus.Cancelled)
            .ToArray();
        return Task.FromResult(outstanding);
    }

    public Task<CustomerInvoiceDto?> GetInvoiceAsync(Guid invoiceId)
    {
        _invoices.TryGetValue(invoiceId, out var invoice);
        return Task.FromResult(invoice);
    }

    public Task<CustomerInvoiceDto> CreateInvoiceAsync(CreateCustomerInvoiceRequest request)
    {
        if (!_customers.TryGetValue(request.CustomerId, out var customer))
        {
            throw new InvalidOperationException("Customer not found");
        }

        var lines = request.Lines.Select((l, i) => new CustomerInvoiceLineDto
        {
            LineNumber = i + 1,
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LineTotal = l.Quantity * l.UnitPrice
        }).ToArray();

        var subtotal = lines.Sum(l => l.LineTotal);
        var tax = subtotal * 0.08m;
        var total = subtotal + tax;

        var invoice = new CustomerInvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{++_invoiceCounter}",
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            Status = CustomerInvoiceStatus.Draft,
            Lines = lines,
            Subtotal = subtotal,
            Tax = tax,
            Total = total,
            AmountPaid = 0,
            Balance = total,
            Currency = "USD"
        };

        _invoices[invoice.Id] = invoice;
        Console.WriteLine($"[AccountsReceivable] Created invoice: {invoice.InvoiceNumber} for {customer.Name}");
        return Task.FromResult(invoice);
    }

    public Task<CustomerInvoiceDto> SendInvoiceAsync(Guid invoiceId)
    {
        if (!_invoices.TryGetValue(invoiceId, out var invoice))
        {
            throw new InvalidOperationException("Invoice not found");
        }

        var updated = invoice with { Status = CustomerInvoiceStatus.Sent };
        _invoices[invoiceId] = updated;
        Console.WriteLine($"[AccountsReceivable] Sent invoice: {invoice.InvoiceNumber}");
        return Task.FromResult(updated);
    }

    public Task<ReceiptDto> RecordReceiptAsync(RecordReceiptRequest request)
    {
        if (!_invoices.TryGetValue(request.InvoiceId, out var invoice))
        {
            throw new InvalidOperationException("Invoice not found");
        }

        var receipt = new ReceiptDto
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = $"RCP-{++_receiptCounter}",
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.CustomerName,
            ReceiptDate = request.ReceiptDate,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Reference = request.Reference,
            Currency = "USD"
        };

        _receipts[receipt.Id] = receipt;

        // Update invoice
        var newAmountPaid = invoice.AmountPaid + request.Amount;
        var newBalance = invoice.Total - newAmountPaid;
        var newStatus = newBalance <= 0
            ? CustomerInvoiceStatus.Paid
            : CustomerInvoiceStatus.PartiallyPaid;

        _invoices[invoice.Id] = invoice with
        {
            AmountPaid = newAmountPaid,
            Balance = newBalance,
            Status = newStatus
        };

        Console.WriteLine($"[AccountsReceivable] Recorded receipt: {receipt.ReceiptNumber} for {receipt.Amount:C}");
        return Task.FromResult(receipt);
    }

    public Task<ReceiptDto[]> GetReceiptsAsync(Guid customerId)
    {
        var receipts = _receipts.Values.Where(r => r.CustomerId == customerId).ToArray();
        return Task.FromResult(receipts);
    }

    public Task<AgingReportDto> GetAgingReportAsync()
    {
        var outstanding = _invoices.Values
            .Where(i => i.Status != CustomerInvoiceStatus.Paid && i.Status != CustomerInvoiceStatus.Cancelled)
            .ToList();

        var today = DateTime.Today;
        decimal current = 0, days30 = 0, days60 = 0, days90 = 0, over90 = 0;
        var customerBuckets = new Dictionary<Guid, AgingBucketDto>();

        foreach (var inv in outstanding)
        {
            var daysOld = (today - inv.DueDate).Days;
            var balance = inv.Balance;

            if (daysOld <= 0) current += balance;
            else if (daysOld <= 30) days30 += balance;
            else if (daysOld <= 60) days60 += balance;
            else if (daysOld <= 90) days90 += balance;
            else over90 += balance;

            // Customer bucket
            if (!customerBuckets.TryGetValue(inv.CustomerId, out var bucket))
            {
                bucket = new AgingBucketDto
                {
                    CustomerId = inv.CustomerId,
                    CustomerName = inv.CustomerName
                };
                customerBuckets[inv.CustomerId] = bucket;
            }

            if (daysOld <= 0) bucket.Current += balance;
            else if (daysOld <= 30) bucket.Days1to30 += balance;
            else if (daysOld <= 60) bucket.Days31to60 += balance;
            else if (daysOld <= 90) bucket.Days61to90 += balance;
            else bucket.Over90Days += balance;
            bucket.Total += balance;
        }

        return Task.FromResult(new AgingReportDto
        {
            AsOfDate = today,
            TotalCurrent = current,
            Total1to30 = days30,
            Total31to60 = days60,
            Total61to90 = days90,
            TotalOver90 = over90,
            GrandTotal = current + days30 + days60 + days90 + over90,
            CustomerBuckets = customerBuckets.Values.ToArray()
        });
    }
}
