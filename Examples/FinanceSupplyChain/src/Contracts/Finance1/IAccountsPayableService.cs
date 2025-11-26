using MessagePack;

namespace FinanceSupplyChain.Contracts.Finance1;

/// <summary>
/// Accounts Payable service interface - Vendor invoice and payment management
/// </summary>
public interface IAccountsPayableService
{
    Task<VendorDto[]> GetVendorsAsync();
    Task<VendorDto> GetVendorAsync(Guid vendorId);
    Task<VendorDto> CreateVendorAsync(CreateVendorRequest request);
    Task<InvoiceDto[]> GetInvoicesAsync(InvoiceStatus? status = null);
    Task<InvoiceDto> GetInvoiceAsync(Guid invoiceId);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<InvoiceDto> ApproveInvoiceAsync(Guid invoiceId);
    Task<PaymentDto> SchedulePaymentAsync(SchedulePaymentRequest request);
    Task<PaymentDto[]> GetScheduledPaymentsAsync(DateTime fromDate, DateTime toDate);
    Task<decimal> GetOutstandingPayablesAsync();
}

[MessagePackObject]
public class VendorDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string VendorCode { get; set; } = string.Empty;
    [Key(2)] public string Name { get; set; } = string.Empty;
    [Key(3)] public string? TaxId { get; set; }
    [Key(4)] public PaymentTerms PaymentTerms { get; set; }
    [Key(5)] public Guid PayableAccountId { get; set; }
    [Key(6)] public bool IsActive { get; set; }
    [Key(7)] public decimal OutstandingBalance { get; set; }
}

[MessagePackObject]
public class CreateVendorRequest
{
    [Key(0)] public string VendorCode { get; set; } = string.Empty;
    [Key(1)] public string Name { get; set; } = string.Empty;
    [Key(2)] public string? TaxId { get; set; }
    [Key(3)] public PaymentTerms PaymentTerms { get; set; }
}

[MessagePackObject]
public class InvoiceDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string InvoiceNumber { get; set; } = string.Empty;
    [Key(2)] public Guid VendorId { get; set; }
    [Key(3)] public string VendorName { get; set; } = string.Empty;
    [Key(4)] public DateTime InvoiceDate { get; set; }
    [Key(5)] public DateTime DueDate { get; set; }
    [Key(6)] public decimal Amount { get; set; }
    [Key(7)] public decimal PaidAmount { get; set; }
    [Key(8)] public InvoiceStatus Status { get; set; }
    [Key(9)] public InvoiceLineDto[] Lines { get; set; } = Array.Empty<InvoiceLineDto>();
    [Key(10)] public Guid? PurchaseOrderId { get; set; }
}

[MessagePackObject]
public class InvoiceLineDto
{
    [Key(0)] public string Description { get; set; } = string.Empty;
    [Key(1)] public int Quantity { get; set; }
    [Key(2)] public decimal UnitPrice { get; set; }
    [Key(3)] public decimal Amount { get; set; }
    [Key(4)] public Guid ExpenseAccountId { get; set; }
}

[MessagePackObject]
public class CreateInvoiceRequest
{
    [Key(0)] public Guid VendorId { get; set; }
    [Key(1)] public string InvoiceNumber { get; set; } = string.Empty;
    [Key(2)] public DateTime InvoiceDate { get; set; }
    [Key(3)] public InvoiceLineDto[] Lines { get; set; } = Array.Empty<InvoiceLineDto>();
    [Key(4)] public Guid? PurchaseOrderId { get; set; }
}

[MessagePackObject]
public class PaymentDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string PaymentNumber { get; set; } = string.Empty;
    [Key(2)] public Guid VendorId { get; set; }
    [Key(3)] public string VendorName { get; set; } = string.Empty;
    [Key(4)] public decimal Amount { get; set; }
    [Key(5)] public DateTime ScheduledDate { get; set; }
    [Key(6)] public DateTime? ExecutedDate { get; set; }
    [Key(7)] public PaymentStatus Status { get; set; }
    [Key(8)] public PaymentMethod Method { get; set; }
    [Key(9)] public Guid[] InvoiceIds { get; set; } = Array.Empty<Guid>();
}

[MessagePackObject]
public class SchedulePaymentRequest
{
    [Key(0)] public Guid VendorId { get; set; }
    [Key(1)] public Guid[] InvoiceIds { get; set; } = Array.Empty<Guid>();
    [Key(2)] public DateTime ScheduledDate { get; set; }
    [Key(3)] public PaymentMethod Method { get; set; }
}

public enum PaymentTerms
{
    Net30 = 30,
    Net60 = 60,
    Net90 = 90,
    DueOnReceipt = 0
}

public enum InvoiceStatus
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5
}

public enum PaymentStatus
{
    Scheduled = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public enum PaymentMethod
{
    BankTransfer = 1,
    Check = 2,
    Wire = 3,
    ACH = 4
}
