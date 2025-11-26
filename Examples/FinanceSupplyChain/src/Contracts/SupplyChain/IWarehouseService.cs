using MessagePack;

namespace FinanceSupplyChain.Contracts.SupplyChain;

/// <summary>
/// Warehouse service interface - Warehouse management and operations
/// </summary>
public interface IWarehouseService
{
    Task<WarehouseDto[]> GetWarehousesAsync();
    Task<WarehouseDto> GetWarehouseAsync(Guid warehouseId);
    Task<WarehouseZoneDto[]> GetZonesAsync(Guid warehouseId);
    Task<WarehouseLocationDto[]> GetLocationsAsync(Guid warehouseId);
    Task<WarehouseLocationDto[]> GetAvailableLocationsAsync(Guid warehouseId, Guid itemId);
    Task<PickListDto[]> GetPendingPickListsAsync(Guid warehouseId);
    Task<PickListDto> GetPickListAsync(Guid pickListId);
    Task<PickListDto> CompletePickListAsync(Guid pickListId, PickConfirmationDto[] confirmations);
    Task<PutAwayTaskDto[]> GetPendingPutAwayTasksAsync(Guid warehouseId);
    Task<PutAwayTaskDto> CompletePutAwayAsync(Guid taskId, Guid locationId);
    Task<WarehouseCapacityDto> GetCapacityAsync(Guid warehouseId);
}

[MessagePackObject]
public class WarehouseDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string Code { get; set; } = string.Empty;
    [Key(2)] public string Name { get; set; } = string.Empty;
    [Key(3)] public AddressDto Address { get; set; } = new();
    [Key(4)] public string? ContactName { get; set; }
    [Key(5)] public string? Phone { get; set; }
    [Key(6)] public string? Email { get; set; }
    [Key(7)] public bool IsActive { get; set; }
    [Key(8)] public WarehouseType Type { get; set; }
    [Key(9)] public decimal TotalCapacity { get; set; }
    [Key(10)] public decimal UsedCapacity { get; set; }
    [Key(11)] public string CapacityUnit { get; set; } = "cubic meters";
}

[MessagePackObject]
public class WarehouseZoneDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public Guid WarehouseId { get; set; }
    [Key(2)] public string Code { get; set; } = string.Empty;
    [Key(3)] public string Name { get; set; } = string.Empty;
    [Key(4)] public ZoneType Type { get; set; }
    [Key(5)] public decimal? MinTemperature { get; set; }
    [Key(6)] public decimal? MaxTemperature { get; set; }
    [Key(7)] public int LocationCount { get; set; }
}

[MessagePackObject]
public class WarehouseLocationDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public Guid WarehouseId { get; set; }
    [Key(2)] public Guid ZoneId { get; set; }
    [Key(3)] public string LocationCode { get; set; } = string.Empty;
    [Key(4)] public string Aisle { get; set; } = string.Empty;
    [Key(5)] public string Rack { get; set; } = string.Empty;
    [Key(6)] public string Level { get; set; } = string.Empty;
    [Key(7)] public string Bin { get; set; } = string.Empty;
    [Key(8)] public decimal MaxCapacity { get; set; }
    [Key(9)] public decimal UsedCapacity { get; set; }
    [Key(10)] public decimal AvailableCapacity { get; set; }
    [Key(11)] public LocationStatus Status { get; set; }
    [Key(12)] public Guid? CurrentItemId { get; set; }
    [Key(13)] public string? CurrentItemCode { get; set; }
    [Key(14)] public decimal? CurrentQuantity { get; set; }
}

[MessagePackObject]
public class PickListDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string PickListNumber { get; set; } = string.Empty;
    [Key(2)] public Guid WarehouseId { get; set; }
    [Key(3)] public string WarehouseName { get; set; } = string.Empty;
    [Key(4)] public DateTime CreatedAt { get; set; }
    [Key(5)] public DateTime? DueDate { get; set; }
    [Key(6)] public PickListStatus Status { get; set; }
    [Key(7)] public string? AssignedTo { get; set; }
    [Key(8)] public string? OrderReference { get; set; }
    [Key(9)] public PickListLineDto[] Lines { get; set; } = Array.Empty<PickListLineDto>();
    [Key(10)] public int TotalLines { get; set; }
    [Key(11)] public int CompletedLines { get; set; }
}

[MessagePackObject]
public class PickListLineDto
{
    [Key(0)] public int LineNumber { get; set; }
    [Key(1)] public Guid ItemId { get; set; }
    [Key(2)] public string ItemCode { get; set; } = string.Empty;
    [Key(3)] public string ItemName { get; set; } = string.Empty;
    [Key(4)] public decimal RequestedQuantity { get; set; }
    [Key(5)] public decimal PickedQuantity { get; set; }
    [Key(6)] public Guid LocationId { get; set; }
    [Key(7)] public string LocationCode { get; set; } = string.Empty;
    [Key(8)] public PickLineStatus Status { get; set; }
}

[MessagePackObject]
public class PickConfirmationDto
{
    [Key(0)] public int LineNumber { get; set; }
    [Key(1)] public decimal PickedQuantity { get; set; }
    [Key(2)] public Guid? ActualLocationId { get; set; }
    [Key(3)] public string? Notes { get; set; }
}

[MessagePackObject]
public class PutAwayTaskDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string TaskNumber { get; set; } = string.Empty;
    [Key(2)] public Guid WarehouseId { get; set; }
    [Key(3)] public Guid ItemId { get; set; }
    [Key(4)] public string ItemCode { get; set; } = string.Empty;
    [Key(5)] public string ItemName { get; set; } = string.Empty;
    [Key(6)] public decimal Quantity { get; set; }
    [Key(7)] public string? LotNumber { get; set; }
    [Key(8)] public DateTime? ExpiryDate { get; set; }
    [Key(9)] public Guid? SuggestedLocationId { get; set; }
    [Key(10)] public string? SuggestedLocationCode { get; set; }
    [Key(11)] public PutAwayStatus Status { get; set; }
    [Key(12)] public string? SourceDocument { get; set; }
    [Key(13)] public DateTime CreatedAt { get; set; }
}

[MessagePackObject]
public class WarehouseCapacityDto
{
    [Key(0)] public Guid WarehouseId { get; set; }
    [Key(1)] public string WarehouseName { get; set; } = string.Empty;
    [Key(2)] public decimal TotalCapacity { get; set; }
    [Key(3)] public decimal UsedCapacity { get; set; }
    [Key(4)] public decimal AvailableCapacity { get; set; }
    [Key(5)] public decimal UtilizationPercent { get; set; }
    [Key(6)] public int TotalLocations { get; set; }
    [Key(7)] public int OccupiedLocations { get; set; }
    [Key(8)] public int EmptyLocations { get; set; }
    [Key(9)] public ZoneCapacityDto[] ByZone { get; set; } = Array.Empty<ZoneCapacityDto>();
}

[MessagePackObject]
public class ZoneCapacityDto
{
    [Key(0)] public Guid ZoneId { get; set; }
    [Key(1)] public string ZoneName { get; set; } = string.Empty;
    [Key(2)] public decimal TotalCapacity { get; set; }
    [Key(3)] public decimal UsedCapacity { get; set; }
    [Key(4)] public decimal UtilizationPercent { get; set; }
}

public enum WarehouseType
{
    General = 1,
    ColdStorage = 2,
    Hazmat = 3,
    Bonded = 4,
    Distribution = 5
}

public enum ZoneType
{
    Receiving = 1,
    Storage = 2,
    Picking = 3,
    Packing = 4,
    Shipping = 5,
    Returns = 6,
    Quarantine = 7
}

public enum LocationStatus
{
    Available = 1,
    Occupied = 2,
    Reserved = 3,
    Blocked = 4
}

public enum PickListStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum PickLineStatus
{
    Pending = 1,
    Picked = 2,
    ShortPicked = 3,
    Cancelled = 4
}

public enum PutAwayStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
