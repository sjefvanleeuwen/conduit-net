using MessagePack;

namespace FinanceSupplyChain.Contracts.Finance1;

/// <summary>
/// Accounts Receivable service interface - Customer invoice and collection management
/// </summary>
public interface IAccountsReceivableService
{
    Task<CustomerDto[]> GetCustomersAsync();
    Task<CustomerDto> GetCustomerAsync(Guid customerId);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request);
    Task<CustomerInvoiceDto[]> GetCustomerInvoicesAsync(CustomerInvoiceStatus? status = null);
    Task<CustomerInvoiceDto> GetCustomerInvoiceAsync(Guid invoiceId);
    Task<CustomerInvoiceDto> CreateCustomerInvoiceAsync(CreateCustomerInvoiceRequest request);
    Task<CustomerInvoiceDto> SendInvoiceAsync(Guid invoiceId);
    Task<ReceiptDto> RecordPaymentAsync(RecordPaymentRequest request);
    Task<decimal> GetOutstandingReceivablesAsync();
    Task<AgingReportDto> GetAgingReportAsync();
}

[MessagePackObject]
public class CustomerDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string CustomerCode { get; set; } = string.Empty;
    [Key(2)] public string Name { get; set; } = string.Empty;
    [Key(3)] public string? Email { get; set; }
    [Key(4)] public string? TaxId { get; set; }
    [Key(5)] public PaymentTerms PaymentTerms { get; set; }
    [Key(6)] public decimal CreditLimit { get; set; }
    [Key(7)] public decimal OutstandingBalance { get; set; }
    [Key(8)] public bool IsActive { get; set; }
}

[MessagePackObject]
public class CreateCustomerRequest
{
    [Key(0)] public string CustomerCode { get; set; } = string.Empty;
    [Key(1)] public string Name { get; set; } = string.Empty;
    [Key(2)] public string? Email { get; set; }
    [Key(3)] public PaymentTerms PaymentTerms { get; set; }
    [Key(4)] public decimal CreditLimit { get; set; }
}

[MessagePackObject]
public class CustomerInvoiceDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string InvoiceNumber { get; set; } = string.Empty;
    [Key(2)] public Guid CustomerId { get; set; }
    [Key(3)] public string CustomerName { get; set; } = string.Empty;
    [Key(4)] public DateTime InvoiceDate { get; set; }
    [Key(5)] public DateTime DueDate { get; set; }
    [Key(6)] public decimal Amount { get; set; }
    [Key(7)] public decimal PaidAmount { get; set; }
    [Key(8)] public CustomerInvoiceStatus Status { get; set; }
    [Key(9)] public CustomerInvoiceLineDto[] Lines { get; set; } = Array.Empty<CustomerInvoiceLineDto>();
}

[MessagePackObject]
public class CustomerInvoiceLineDto
{
    [Key(0)] public string Description { get; set; } = string.Empty;
    [Key(1)] public int Quantity { get; set; }
    [Key(2)] public decimal UnitPrice { get; set; }
    [Key(3)] public decimal Amount { get; set; }
    [Key(4)] public Guid RevenueAccountId { get; set; }
}

[MessagePackObject]
public class CreateCustomerInvoiceRequest
{
    [Key(0)] public Guid CustomerId { get; set; }
    [Key(1)] public CustomerInvoiceLineDto[] Lines { get; set; } = Array.Empty<CustomerInvoiceLineDto>();
}

[MessagePackObject]
public class ReceiptDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string ReceiptNumber { get; set; } = string.Empty;
    [Key(2)] public Guid CustomerId { get; set; }
    [Key(3)] public decimal Amount { get; set; }
    [Key(4)] public DateTime ReceivedDate { get; set; }
    [Key(5)] public string? Reference { get; set; }
    [Key(6)] public Guid[] InvoiceIds { get; set; } = Array.Empty<Guid>();
}

[MessagePackObject]
public class RecordPaymentRequest
{
    [Key(0)] public Guid CustomerId { get; set; }
    [Key(1)] public decimal Amount { get; set; }
    [Key(2)] public DateTime ReceivedDate { get; set; }
    [Key(3)] public string? Reference { get; set; }
    [Key(4)] public Guid[] InvoiceIds { get; set; } = Array.Empty<Guid>();
}

[MessagePackObject]
public class AgingReportDto
{
    [Key(0)] public DateTime AsOfDate { get; set; }
    [Key(1)] public AgingBucketDto[] Buckets { get; set; } = Array.Empty<AgingBucketDto>();
    [Key(2)] public decimal TotalOutstanding { get; set; }
}

[MessagePackObject]
public class AgingBucketDto
{
    [Key(0)] public string Label { get; set; } = string.Empty;
    [Key(1)] public int DaysFrom { get; set; }
    [Key(2)] public int DaysTo { get; set; }
    [Key(3)] public decimal Amount { get; set; }
    [Key(4)] public int InvoiceCount { get; set; }
}

public enum CustomerInvoiceStatus
{
    Draft = 0,
    Sent = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}
