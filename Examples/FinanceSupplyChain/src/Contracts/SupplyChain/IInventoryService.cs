using MessagePack;

namespace FinanceSupplyChain.Contracts.SupplyChain;

/// <summary>
/// Inventory service interface - Stock management and tracking
/// </summary>
public interface IInventoryService
{
    Task<InventoryItemDto[]> GetItemsAsync();
    Task<InventoryItemDto> GetItemAsync(Guid itemId);
    Task<InventoryItemDto> GetItemByCodeAsync(string itemCode);
    Task<StockLevelDto[]> GetStockLevelsAsync();
    Task<StockLevelDto> GetStockLevelAsync(Guid itemId);
    Task<StockLevelDto[]> GetLowStockItemsAsync();
    Task<StockMovementDto[]> GetStockMovementsAsync(Guid itemId, DateTime fromDate, DateTime toDate);
    Task<StockAdjustmentDto> AdjustStockAsync(AdjustStockRequest request);
    Task<StockValuationDto> GetStockValuationAsync();
    Task<StockValuationDto> GetStockValuationAsync(Guid warehouseId);
}

[MessagePackObject]
public class InventoryItemDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string ItemCode { get; set; } = string.Empty;
    [Key(2)] public string Name { get; set; } = string.Empty;
    [Key(3)] public string Description { get; set; } = string.Empty;
    [Key(4)] public string Category { get; set; } = string.Empty;
    [Key(5)] public string Unit { get; set; } = string.Empty;
    [Key(6)] public decimal UnitCost { get; set; }
    [Key(7)] public decimal SellingPrice { get; set; }
    [Key(8)] public decimal ReorderPoint { get; set; }
    [Key(9)] public decimal ReorderQuantity { get; set; }
    [Key(10)] public int LeadTimeDays { get; set; }
    [Key(11)] public bool IsActive { get; set; }
    [Key(12)] public string? Barcode { get; set; }
    [Key(13)] public decimal Weight { get; set; }
    [Key(14)] public string? WeightUnit { get; set; }
    [Key(15)] public ItemDimensionsDto? Dimensions { get; set; }
}

[MessagePackObject]
public class ItemDimensionsDto
{
    [Key(0)] public decimal Length { get; set; }
    [Key(1)] public decimal Width { get; set; }
    [Key(2)] public decimal Height { get; set; }
    [Key(3)] public string Unit { get; set; } = "cm";
}

[MessagePackObject]
public class StockLevelDto
{
    [Key(0)] public Guid ItemId { get; set; }
    [Key(1)] public string ItemCode { get; set; } = string.Empty;
    [Key(2)] public string ItemName { get; set; } = string.Empty;
    [Key(3)] public decimal OnHand { get; set; }
    [Key(4)] public decimal Reserved { get; set; }
    [Key(5)] public decimal Available { get; set; }
    [Key(6)] public decimal OnOrder { get; set; }
    [Key(7)] public decimal ReorderPoint { get; set; }
    [Key(8)] public StockStatus Status { get; set; }
    [Key(9)] public WarehouseStockDto[] ByWarehouse { get; set; } = Array.Empty<WarehouseStockDto>();
}

[MessagePackObject]
public class WarehouseStockDto
{
    [Key(0)] public Guid WarehouseId { get; set; }
    [Key(1)] public string WarehouseName { get; set; } = string.Empty;
    [Key(2)] public decimal OnHand { get; set; }
    [Key(3)] public decimal Reserved { get; set; }
    [Key(4)] public decimal Available { get; set; }
    [Key(5)] public string? Location { get; set; }
}

[MessagePackObject]
public class StockMovementDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public Guid ItemId { get; set; }
    [Key(2)] public string ItemCode { get; set; } = string.Empty;
    [Key(3)] public DateTime MovementDate { get; set; }
    [Key(4)] public MovementType Type { get; set; }
    [Key(5)] public decimal Quantity { get; set; }
    [Key(6)] public decimal BalanceBefore { get; set; }
    [Key(7)] public decimal BalanceAfter { get; set; }
    [Key(8)] public Guid? WarehouseId { get; set; }
    [Key(9)] public string? Reference { get; set; }
    [Key(10)] public string? Notes { get; set; }
    [Key(11)] public string CreatedBy { get; set; } = string.Empty;
}

[MessagePackObject]
public class AdjustStockRequest
{
    [Key(0)] public Guid ItemId { get; set; }
    [Key(1)] public Guid WarehouseId { get; set; }
    [Key(2)] public decimal Quantity { get; set; }
    [Key(3)] public AdjustmentReason Reason { get; set; }
    [Key(4)] public string? Notes { get; set; }
}

[MessagePackObject]
public class StockAdjustmentDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public Guid ItemId { get; set; }
    [Key(2)] public string ItemCode { get; set; } = string.Empty;
    [Key(3)] public Guid WarehouseId { get; set; }
    [Key(4)] public DateTime AdjustmentDate { get; set; }
    [Key(5)] public decimal QuantityBefore { get; set; }
    [Key(6)] public decimal AdjustmentQuantity { get; set; }
    [Key(7)] public decimal QuantityAfter { get; set; }
    [Key(8)] public AdjustmentReason Reason { get; set; }
    [Key(9)] public string? Notes { get; set; }
    [Key(10)] public string AdjustedBy { get; set; } = string.Empty;
}

[MessagePackObject]
public class StockValuationDto
{
    [Key(0)] public DateTime AsOfDate { get; set; }
    [Key(1)] public Guid? WarehouseId { get; set; }
    [Key(2)] public string? WarehouseName { get; set; }
    [Key(3)] public int TotalItems { get; set; }
    [Key(4)] public decimal TotalUnits { get; set; }
    [Key(5)] public decimal TotalValue { get; set; }
    [Key(6)] public string Currency { get; set; } = "USD";
    [Key(7)] public StockValuationItemDto[] Items { get; set; } = Array.Empty<StockValuationItemDto>();
}

[MessagePackObject]
public class StockValuationItemDto
{
    [Key(0)] public Guid ItemId { get; set; }
    [Key(1)] public string ItemCode { get; set; } = string.Empty;
    [Key(2)] public string ItemName { get; set; } = string.Empty;
    [Key(3)] public decimal Quantity { get; set; }
    [Key(4)] public decimal UnitCost { get; set; }
    [Key(5)] public decimal TotalValue { get; set; }
    [Key(6)] public decimal ValuePercent { get; set; }
}

public enum StockStatus
{
    InStock = 1,
    LowStock = 2,
    OutOfStock = 3,
    Overstock = 4
}

public enum MovementType
{
    Receipt = 1,
    Issue = 2,
    Transfer = 3,
    Adjustment = 4,
    Return = 5,
    Scrap = 6
}

public enum AdjustmentReason
{
    PhysicalCount = 1,
    Damage = 2,
    Expiry = 3,
    Theft = 4,
    Error = 5,
    Other = 6
}
