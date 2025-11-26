using MessagePack;

namespace FinanceSupplyChain.Contracts.SupplyChain;

/// <summary>
/// Logistics service interface - Shipping and delivery management
/// </summary>
public interface ILogisticsService
{
    Task<ShipmentDto[]> GetShipmentsAsync();
    Task<ShipmentDto> GetShipmentAsync(Guid shipmentId);
    Task<ShipmentDto> GetShipmentByTrackingAsync(string trackingNumber);
    Task<ShipmentDto> CreateShipmentAsync(CreateShipmentRequest request);
    Task<ShipmentDto> UpdateShipmentStatusAsync(Guid shipmentId, ShipmentStatus status, string? notes);
    Task<CarrierDto[]> GetCarriersAsync();
    Task<ShippingRateDto[]> GetShippingRatesAsync(ShippingRateRequest request);
    Task<DeliveryRouteDto[]> GetDeliveryRoutesAsync(DateTime date);
    Task<TrackingEventDto[]> GetTrackingHistoryAsync(string trackingNumber);
}

[MessagePackObject]
public class ShipmentDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string ShipmentNumber { get; set; } = string.Empty;
    [Key(2)] public string? TrackingNumber { get; set; }
    [Key(3)] public Guid? OrderId { get; set; }
    [Key(4)] public string? OrderReference { get; set; }
    [Key(5)] public Guid CarrierId { get; set; }
    [Key(6)] public string CarrierName { get; set; } = string.Empty;
    [Key(7)] public string ServiceType { get; set; } = string.Empty;
    [Key(8)] public ShipmentStatus Status { get; set; }
    [Key(9)] public AddressDto ShipFrom { get; set; } = new();
    [Key(10)] public AddressDto ShipTo { get; set; } = new();
    [Key(11)] public DateTime CreatedAt { get; set; }
    [Key(12)] public DateTime? ShippedAt { get; set; }
    [Key(13)] public DateTime? EstimatedDelivery { get; set; }
    [Key(14)] public DateTime? ActualDelivery { get; set; }
    [Key(15)] public ShipmentPackageDto[] Packages { get; set; } = Array.Empty<ShipmentPackageDto>();
    [Key(16)] public decimal TotalWeight { get; set; }
    [Key(17)] public string WeightUnit { get; set; } = "kg";
    [Key(18)] public decimal ShippingCost { get; set; }
    [Key(19)] public string Currency { get; set; } = "USD";
    [Key(20)] public string? SpecialInstructions { get; set; }
}

[MessagePackObject]
public class ShipmentPackageDto
{
    [Key(0)] public int PackageNumber { get; set; }
    [Key(1)] public string? PackageTrackingNumber { get; set; }
    [Key(2)] public decimal Weight { get; set; }
    [Key(3)] public decimal Length { get; set; }
    [Key(4)] public decimal Width { get; set; }
    [Key(5)] public decimal Height { get; set; }
    [Key(6)] public string DimensionUnit { get; set; } = "cm";
    [Key(7)] public ShipmentItemDto[] Items { get; set; } = Array.Empty<ShipmentItemDto>();
}

[MessagePackObject]
public class ShipmentItemDto
{
    [Key(0)] public Guid ItemId { get; set; }
    [Key(1)] public string ItemCode { get; set; } = string.Empty;
    [Key(2)] public string Description { get; set; } = string.Empty;
    [Key(3)] public decimal Quantity { get; set; }
    [Key(4)] public string Unit { get; set; } = string.Empty;
}

[MessagePackObject]
public class CreateShipmentRequest
{
    [Key(0)] public Guid? OrderId { get; set; }
    [Key(1)] public Guid CarrierId { get; set; }
    [Key(2)] public string ServiceType { get; set; } = string.Empty;
    [Key(3)] public AddressDto ShipFrom { get; set; } = new();
    [Key(4)] public AddressDto ShipTo { get; set; } = new();
    [Key(5)] public CreatePackageRequest[] Packages { get; set; } = Array.Empty<CreatePackageRequest>();
    [Key(6)] public string? SpecialInstructions { get; set; }
}

[MessagePackObject]
public class CreatePackageRequest
{
    [Key(0)] public decimal Weight { get; set; }
    [Key(1)] public decimal Length { get; set; }
    [Key(2)] public decimal Width { get; set; }
    [Key(3)] public decimal Height { get; set; }
    [Key(4)] public CreateShipmentItemRequest[] Items { get; set; } = Array.Empty<CreateShipmentItemRequest>();
}

[MessagePackObject]
public class CreateShipmentItemRequest
{
    [Key(0)] public Guid ItemId { get; set; }
    [Key(1)] public decimal Quantity { get; set; }
}

[MessagePackObject]
public class CarrierDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string Code { get; set; } = string.Empty;
    [Key(2)] public string Name { get; set; } = string.Empty;
    [Key(3)] public string? Website { get; set; }
    [Key(4)] public string? TrackingUrl { get; set; }
    [Key(5)] public string[] ServiceTypes { get; set; } = Array.Empty<string>();
    [Key(6)] public bool IsActive { get; set; }
    [Key(7)] public string? ContactEmail { get; set; }
    [Key(8)] public string? ContactPhone { get; set; }
}

[MessagePackObject]
public class ShippingRateRequest
{
    [Key(0)] public AddressDto FromAddress { get; set; } = new();
    [Key(1)] public AddressDto ToAddress { get; set; } = new();
    [Key(2)] public decimal TotalWeight { get; set; }
    [Key(3)] public PackageDimensionDto[] Packages { get; set; } = Array.Empty<PackageDimensionDto>();
}

[MessagePackObject]
public class PackageDimensionDto
{
    [Key(0)] public decimal Length { get; set; }
    [Key(1)] public decimal Width { get; set; }
    [Key(2)] public decimal Height { get; set; }
    [Key(3)] public decimal Weight { get; set; }
}

[MessagePackObject]
public class ShippingRateDto
{
    [Key(0)] public Guid CarrierId { get; set; }
    [Key(1)] public string CarrierName { get; set; } = string.Empty;
    [Key(2)] public string ServiceType { get; set; } = string.Empty;
    [Key(3)] public decimal Rate { get; set; }
    [Key(4)] public string Currency { get; set; } = "USD";
    [Key(5)] public int EstimatedDays { get; set; }
    [Key(6)] public DateTime? EstimatedDelivery { get; set; }
    [Key(7)] public bool IsGuaranteed { get; set; }
}

[MessagePackObject]
public class DeliveryRouteDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public string RouteNumber { get; set; } = string.Empty;
    [Key(2)] public DateTime RouteDate { get; set; }
    [Key(3)] public string? DriverName { get; set; }
    [Key(4)] public string? VehicleNumber { get; set; }
    [Key(5)] public RouteStatus Status { get; set; }
    [Key(6)] public int TotalStops { get; set; }
    [Key(7)] public int CompletedStops { get; set; }
    [Key(8)] public RouteStopDto[] Stops { get; set; } = Array.Empty<RouteStopDto>();
    [Key(9)] public decimal TotalDistance { get; set; }
    [Key(10)] public string DistanceUnit { get; set; } = "km";
}

[MessagePackObject]
public class RouteStopDto
{
    [Key(0)] public int Sequence { get; set; }
    [Key(1)] public Guid ShipmentId { get; set; }
    [Key(2)] public string ShipmentNumber { get; set; } = string.Empty;
    [Key(3)] public AddressDto Address { get; set; } = new();
    [Key(4)] public DateTime? EstimatedArrival { get; set; }
    [Key(5)] public DateTime? ActualArrival { get; set; }
    [Key(6)] public StopStatus Status { get; set; }
    [Key(7)] public string? Notes { get; set; }
}

[MessagePackObject]
public class TrackingEventDto
{
    [Key(0)] public Guid Id { get; set; }
    [Key(1)] public DateTime Timestamp { get; set; }
    [Key(2)] public string Status { get; set; } = string.Empty;
    [Key(3)] public string Description { get; set; } = string.Empty;
    [Key(4)] public string? Location { get; set; }
    [Key(5)] public string? SignedBy { get; set; }
}

public enum ShipmentStatus
{
    Created = 1,
    LabelPrinted = 2,
    PickedUp = 3,
    InTransit = 4,
    OutForDelivery = 5,
    Delivered = 6,
    Failed = 7,
    Returned = 8,
    Cancelled = 9
}

public enum RouteStatus
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum StopStatus
{
    Pending = 1,
    Arrived = 2,
    Completed = 3,
    Failed = 4,
    Skipped = 5
}
