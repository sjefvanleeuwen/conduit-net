using System.Collections.Concurrent;
using FinanceSupplyChain.Contracts.Finance1;

namespace AccountsPayable.Node;

public class AccountsPayableService : IAccountsPayableService
{
    private readonly ConcurrentDictionary<Guid, VendorDto> _vendors = new();
    private readonly ConcurrentDictionary<Guid, InvoiceDto> _invoices = new();
    private readonly ConcurrentDictionary<Guid, PaymentDto> _payments = new();
    private int _invoiceCounter = 1000;
    private int _paymentCounter = 1000;

    public AccountsPayableService()
    {
        SeedData();
    }

    private void SeedData()
    {
        var vendors = new[]
        {
            new VendorDto { Id = Guid.NewGuid(), VendorNumber = "V001", Name = "Office Supplies Co", ContactName = "John Smith", Email = "john@officesupplies.com", Phone = "555-0101", Address = "123 Main St, City, ST 12345", PaymentTerms = "Net 30", TaxId = "12-3456789", CreditLimit = 50000m, IsActive = true },
            new VendorDto { Id = Guid.NewGuid(), VendorNumber = "V002", Name = "Tech Hardware Inc", ContactName = "Jane Doe", Email = "jane@techhardware.com", Phone = "555-0102", Address = "456 Tech Blvd, City, ST 12346", PaymentTerms = "Net 45", TaxId = "98-7654321", CreditLimit = 100000m, IsActive = true },
            new VendorDto { Id = Guid.NewGuid(), VendorNumber = "V003", Name = "Logistics Partners", ContactName = "Bob Wilson", Email = "bob@logistics.com", Phone = "555-0103", Address = "789 Transport Ave, City, ST 12347", PaymentTerms = "Net 15", TaxId = "55-1234567", CreditLimit = 25000m, IsActive = true },
        };
        
        foreach (var vendor in vendors)
        {
            _vendors[vendor.Id] = vendor;
        }

        // Create some sample invoices
        var v1 = vendors[0];
        var invoice1 = new InvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{++_invoiceCounter}",
            VendorId = v1.Id,
            VendorName = v1.Name,
            InvoiceDate = DateTime.Today.AddDays(-10),
            DueDate = DateTime.Today.AddDays(20),
            Status = InvoiceStatus.Approved,
            Lines = new[]
            {
                new InvoiceLineDto { LineNumber = 1, Description = "Office Supplies - Paper", Quantity = 100, UnitPrice = 25.00m, LineTotal = 2500.00m, GlAccountId = Guid.NewGuid() },
                new InvoiceLineDto { LineNumber = 2, Description = "Office Supplies - Pens", Quantity = 50, UnitPrice = 10.00m, LineTotal = 500.00m, GlAccountId = Guid.NewGuid() }
            },
            Subtotal = 3000.00m,
            Tax = 240.00m,
            Total = 3240.00m,
            Currency = "USD"
        };
        _invoices[invoice1.Id] = invoice1;
    }

    public Task<VendorDto[]> GetVendorsAsync()
    {
        return Task.FromResult(_vendors.Values.ToArray());
    }

    public Task<VendorDto?> GetVendorAsync(Guid vendorId)
    {
        _vendors.TryGetValue(vendorId, out var vendor);
        return Task.FromResult(vendor);
    }

    public Task<InvoiceDto[]> GetInvoicesAsync()
    {
        return Task.FromResult(_invoices.Values.OrderByDescending(i => i.InvoiceDate).ToArray());
    }

    public Task<InvoiceDto[]> GetInvoicesByVendorAsync(Guid vendorId)
    {
        var invoices = _invoices.Values.Where(i => i.VendorId == vendorId).ToArray();
        return Task.FromResult(invoices);
    }

    public Task<InvoiceDto[]> GetPendingInvoicesAsync()
    {
        var pending = _invoices.Values
            .Where(i => i.Status == InvoiceStatus.Approved || i.Status == InvoiceStatus.PartiallyPaid)
            .ToArray();
        return Task.FromResult(pending);
    }

    public Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId)
    {
        _invoices.TryGetValue(invoiceId, out var invoice);
        return Task.FromResult(invoice);
    }

    public Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        if (!_vendors.TryGetValue(request.VendorId, out var vendor))
        {
            throw new InvalidOperationException("Vendor not found");
        }

        var lines = request.Lines.Select((l, i) => new InvoiceLineDto
        {
            LineNumber = i + 1,
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LineTotal = l.Quantity * l.UnitPrice,
            GlAccountId = l.GlAccountId
        }).ToArray();

        var subtotal = lines.Sum(l => l.LineTotal);
        var tax = subtotal * 0.08m; // 8% tax

        var invoice = new InvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{++_invoiceCounter}",
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            Status = InvoiceStatus.Pending,
            Lines = lines,
            Subtotal = subtotal,
            Tax = tax,
            Total = subtotal + tax,
            Currency = "USD"
        };

        _invoices[invoice.Id] = invoice;
        Console.WriteLine($"[AccountsPayable] Created invoice: {invoice.InvoiceNumber} for {vendor.Name}");
        return Task.FromResult(invoice);
    }

    public Task<InvoiceDto> ApproveInvoiceAsync(Guid invoiceId)
    {
        if (!_invoices.TryGetValue(invoiceId, out var invoice))
        {
            throw new InvalidOperationException("Invoice not found");
        }

        var updated = invoice with { Status = InvoiceStatus.Approved };
        _invoices[invoiceId] = updated;
        Console.WriteLine($"[AccountsPayable] Approved invoice: {invoice.InvoiceNumber}");
        return Task.FromResult(updated);
    }

    public Task<PaymentDto> ProcessPaymentAsync(CreatePaymentRequest request)
    {
        if (!_invoices.TryGetValue(request.InvoiceId, out var invoice))
        {
            throw new InvalidOperationException("Invoice not found");
        }

        var payment = new PaymentDto
        {
            Id = Guid.NewGuid(),
            PaymentNumber = $"PAY-{++_paymentCounter}",
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            VendorId = invoice.VendorId,
            VendorName = invoice.VendorName,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Reference = request.Reference,
            Currency = "USD"
        };

        _payments[payment.Id] = payment;

        // Update invoice status
        var paidAmount = _payments.Values
            .Where(p => p.InvoiceId == invoice.Id)
            .Sum(p => p.Amount);

        var newStatus = paidAmount >= invoice.Total
            ? InvoiceStatus.Paid
            : InvoiceStatus.PartiallyPaid;

        _invoices[invoice.Id] = invoice with { Status = newStatus, AmountPaid = paidAmount };

        Console.WriteLine($"[AccountsPayable] Processed payment: {payment.PaymentNumber} for {payment.Amount:C}");
        return Task.FromResult(payment);
    }

    public Task<PaymentDto[]> GetPaymentHistoryAsync(Guid vendorId)
    {
        var payments = _payments.Values.Where(p => p.VendorId == vendorId).ToArray();
        return Task.FromResult(payments);
    }

    public Task<ApAgingSummaryDto> GetAgingSummaryAsync()
    {
        var pendingInvoices = _invoices.Values
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToList();

        var today = DateTime.Today;
        decimal current = 0, days30 = 0, days60 = 0, days90 = 0, over90 = 0;

        foreach (var inv in pendingInvoices)
        {
            var daysOld = (today - inv.DueDate).Days;
            var balance = inv.Total - inv.AmountPaid;

            if (daysOld <= 0) current += balance;
            else if (daysOld <= 30) days30 += balance;
            else if (daysOld <= 60) days60 += balance;
            else if (daysOld <= 90) days90 += balance;
            else over90 += balance;
        }

        return Task.FromResult(new ApAgingSummaryDto
        {
            AsOfDate = today,
            Current = current,
            Days1to30 = days30,
            Days31to60 = days60,
            Days61to90 = days90,
            Over90Days = over90,
            TotalOutstanding = current + days30 + days60 + days90 + over90
        });
    }
}
