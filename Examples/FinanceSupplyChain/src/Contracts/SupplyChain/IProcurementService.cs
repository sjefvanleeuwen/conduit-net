using MessagePack;

namespace FinanceSupplyChain.Contracts.SupplyChain;

/// <summary>
/// Procurement service interface - Purchasing and supplier management
/// </summary>
public interface IProcurementService
{
    Task<PurchaseOrderDto[]> GetPurchaseOrdersAsync();
    Task<PurchaseOrderDto> GetPurchaseOrderAsync(Guid poId);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request);
    Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(Guid poId, string approverNotes);
    Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(Guid poId, ReceiveItemsRequest[] items);
    Task<SupplierDto[]> GetSuppliersAsync();
    Task<SupplierDto> GetSupplierAsync(Guid supplierId);
    Task<SupplierPerformanceDto> GetSupplierPerformanceAsync(Guid supplierId);
    Task<PurchaseRequisitionDto[]> GetPendingRequisitionsAsync();
}

[MessagePackObject]
public class PurchaseOrderDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string PoNumber { get; set; } = string.Empty;
    [Key(2)] public Guid SupplierId { get; set; }
    [Key(3)] public string SupplierName { get; set; } = string.Empty;
    [Key(4)] public DateTime OrderDate { get; set; }
    [Key(5)] public DateTime? ExpectedDeliveryDate { get; set; }
    [Key(6)] public PurchaseOrderStatus Status { get; set; }
    [Key(7)] public PurchaseOrderLineDto[] Lines { get; set; } = Array.Empty<PurchaseOrderLineDto>();
    [Key(8)] public decimal Subtotal { get; set; }
    [Key(9)] public decimal Tax { get; set; }
    [Key(10)] public decimal Total { get; set; }
    [Key(11)] public string Currency { get; set; } = "USD";
    [Key(12)] public string? Notes { get; set; }
    [Key(13)] public string? ApprovedBy { get; set; }
    [Key(14)] public DateTime? ApprovedDate { get; set; }
}

[MessagePackObject]
public class PurchaseOrderLineDto
{
    [Key(0)] public int LineNumber { get; set; }
    [Key(1)] public Guid ItemId { get; set; }
    [Key(2)] public string ItemCode { get; set; } = string.Empty;
    [Key(3)] public string Description { get; set; } = string.Empty;
    [Key(4)] public decimal Quantity { get; set; }
    [Key(5)] public string Unit { get; set; } = string.Empty;
    [Key(6)] public decimal UnitPrice { get; set; }
    [Key(7)] public decimal LineTotal { get; set; }
    [Key(8)] public decimal ReceivedQuantity { get; set; }
    [Key(9)] public PoLineStatus Status { get; set; }
}

[MessagePackObject]
public class CreatePurchaseOrderRequest
{
    [Key(0)] public Guid SupplierId { get; set; }
    [Key(1)] public DateTime? ExpectedDeliveryDate { get; set; }
    [Key(2)] public CreatePoLineRequest[] Lines { get; set; } = Array.Empty<CreatePoLineRequest>();
    [Key(3)] public string? Notes { get; set; }
}

[MessagePackObject]
public class CreatePoLineRequest
{
    [Key(0)] public Guid ItemId { get; set; }
    [Key(1)] public decimal Quantity { get; set; }
    [Key(2)] public decimal UnitPrice { get; set; }
}

[MessagePackObject]
public class ReceiveItemsRequest
{
    [Key(0)] public int LineNumber { get; set; }
    [Key(1)] public decimal Quantity { get; set; }
    [Key(2)] public string? LotNumber { get; set; }
    [Key(3)] public DateTime? ExpiryDate { get; set; }
}

[MessagePackObject]
public class SupplierDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string Code { get; set; } = string.Empty;
    [Key(2)] public string Name { get; set; } = string.Empty;
    [Key(3)] public string ContactName { get; set; } = string.Empty;
    [Key(4)] public string Email { get; set; } = string.Empty;
    [Key(5)] public string Phone { get; set; } = string.Empty;
    [Key(6)] public AddressDto Address { get; set; } = new();
    [Key(7)] public string PaymentTerms { get; set; } = string.Empty;
    [Key(8)] public decimal CreditLimit { get; set; }
    [Key(9)] public bool IsActive { get; set; }
    [Key(10)] public SupplierRating Rating { get; set; }
}

[MessagePackObject]
public class AddressDto
{
    [Key(0)] public string Street { get; set; } = string.Empty;
    [Key(1)] public string City { get; set; } = string.Empty;
    [Key(2)] public string State { get; set; } = string.Empty;
    [Key(3)] public string PostalCode { get; set; } = string.Empty;
    [Key(4)] public string Country { get; set; } = string.Empty;
}

[MessagePackObject]
public class SupplierPerformanceDto
{
    [Key(0)] public Guid SupplierId { get; set; }
    [Key(1)] public string SupplierName { get; set; } = string.Empty;
    [Key(2)] public int TotalOrders { get; set; }
    [Key(3)] public int OnTimeDeliveries { get; set; }
    [Key(4)] public decimal OnTimeDeliveryPercent { get; set; }
    [Key(5)] public int QualityAcceptances { get; set; }
    [Key(6)] public decimal QualityAcceptancePercent { get; set; }
    [Key(7)] public decimal AverageLeadTimeDays { get; set; }
    [Key(8)] public decimal TotalSpend { get; set; }
    [Key(9)] public SupplierRating OverallRating { get; set; }
}

[MessagePackObject]
public class PurchaseRequisitionDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string RequisitionNumber { get; set; } = string.Empty;
    [Key(2)] public string RequestedBy { get; set; } = string.Empty;
    [Key(3)] public DateTime RequestDate { get; set; }
    [Key(4)] public DateTime NeededByDate { get; set; }
    [Key(5)] public RequisitionStatus Status { get; set; }
    [Key(6)] public RequisitionLineDto[] Lines { get; set; } = Array.Empty<RequisitionLineDto>();
    [Key(7)] public string? Justification { get; set; }
}

[MessagePackObject]
public class RequisitionLineDto
{
    [Key(0)] public int LineNumber { get; set; }
    [Key(1)] public Guid ItemId { get; set; }
    [Key(2)] public string ItemDescription { get; set; } = string.Empty;
    [Key(3)] public decimal Quantity { get; set; }
    [Key(4)] public string Unit { get; set; } = string.Empty;
    [Key(5)] public decimal EstimatedPrice { get; set; }
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Sent = 4,
    PartiallyReceived = 5,
    Received = 6,
    Cancelled = 7,
    Closed = 8
}

public enum PoLineStatus
{
    Open = 1,
    PartiallyReceived = 2,
    Received = 3,
    Cancelled = 4
}

public enum SupplierRating
{
    Excellent = 1,
    Good = 2,
    Acceptable = 3,
    NeedsImprovement = 4,
    Unacceptable = 5,
    NotRated = 6
}

public enum RequisitionStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    ConvertedToPO = 4,
    Rejected = 5,
    Cancelled = 6
}
