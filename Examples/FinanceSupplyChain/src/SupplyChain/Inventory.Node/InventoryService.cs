using System.Collections.Concurrent;
using FinanceSupplyChain.Contracts.SupplyChain;

namespace Inventory.Node;

public class InventoryService : IInventoryService
{
    private readonly ConcurrentDictionary<Guid, InventoryItemDto> _items = new();
    private readonly ConcurrentDictionary<Guid, StockLevelDto> _stockLevels = new();
    private readonly List<StockMovementDto> _movements = new();

    public InventoryService()
    {
        SeedData();
    }

    private void SeedData()
    {
        var warehouseId = Guid.NewGuid();
        var items = new[]
        {
            new InventoryItemDto { Id = Guid.NewGuid(), ItemCode = "SKU001", Name = "Widget A", Description = "Standard widget", Category = "Widgets", Unit = "EA", UnitCost = 10.00m, SellingPrice = 25.00m, ReorderPoint = 100, ReorderQuantity = 500, LeadTimeDays = 7, IsActive = true },
            new InventoryItemDto { Id = Guid.NewGuid(), ItemCode = "SKU002", Name = "Widget B", Description = "Premium widget", Category = "Widgets", Unit = "EA", UnitCost = 25.00m, SellingPrice = 60.00m, ReorderPoint = 50, ReorderQuantity = 200, LeadTimeDays = 14, IsActive = true },
            new InventoryItemDto { Id = Guid.NewGuid(), ItemCode = "SKU003", Name = "Component X", Description = "Electronic component", Category = "Components", Unit = "EA", UnitCost = 5.00m, SellingPrice = 12.00m, ReorderPoint = 500, ReorderQuantity = 2000, LeadTimeDays = 21, IsActive = true },
            new InventoryItemDto { Id = Guid.NewGuid(), ItemCode = "RAW001", Name = "Steel Sheet", Description = "1mm steel sheet", Category = "Raw Materials", Unit = "KG", UnitCost = 2.50m, SellingPrice = 0m, ReorderPoint = 1000, ReorderQuantity = 5000, LeadTimeDays = 10, IsActive = true },
            new InventoryItemDto { Id = Guid.NewGuid(), ItemCode = "PKG001", Name = "Cardboard Box", Description = "Medium shipping box", Category = "Packaging", Unit = "EA", UnitCost = 0.50m, SellingPrice = 0m, ReorderPoint = 200, ReorderQuantity = 1000, LeadTimeDays = 5, IsActive = true },
        };

        foreach (var item in items)
        {
            _items[item.Id] = item;
            
            var onHand = item.ItemCode switch
            {
                "SKU001" => 250m,
                "SKU002" => 30m, // Low stock
                "SKU003" => 1500m,
                "RAW001" => 800m, // Low stock
                "PKG001" => 500m,
                _ => 100m
            };

            var reserved = onHand * 0.1m;
            _stockLevels[item.Id] = new StockLevelDto
            {
                ItemId = item.Id,
                ItemCode = item.ItemCode,
                ItemName = item.Name,
                OnHand = onHand,
                Reserved = reserved,
                Available = onHand - reserved,
                OnOrder = item.ItemCode == "SKU002" ? 200m : 0m,
                ReorderPoint = item.ReorderPoint,
                Status = onHand <= item.ReorderPoint ? StockStatus.LowStock : StockStatus.InStock,
                ByWarehouse = new[]
                {
                    new WarehouseStockDto
                    {
                        WarehouseId = warehouseId,
                        WarehouseName = "Main Warehouse",
                        OnHand = onHand,
                        Reserved = reserved,
                        Available = onHand - reserved,
                        Location = "A-01-01"
                    }
                }
            };
        }
    }

    public Task<InventoryItemDto[]> GetItemsAsync()
    {
        return Task.FromResult(_items.Values.ToArray());
    }

    public Task<InventoryItemDto> GetItemAsync(Guid itemId)
    {
        _items.TryGetValue(itemId, out var item);
        return Task.FromResult(item!);
    }

    public Task<InventoryItemDto> GetItemByCodeAsync(string itemCode)
    {
        var item = _items.Values.FirstOrDefault(i => i.ItemCode == itemCode);
        return Task.FromResult(item!);
    }

    public Task<StockLevelDto[]> GetStockLevelsAsync()
    {
        return Task.FromResult(_stockLevels.Values.ToArray());
    }

    public Task<StockLevelDto> GetStockLevelAsync(Guid itemId)
    {
        _stockLevels.TryGetValue(itemId, out var level);
        return Task.FromResult(level!);
    }

    public Task<StockLevelDto[]> GetLowStockItemsAsync()
    {
        var lowStock = _stockLevels.Values
            .Where(s => s.Status == StockStatus.LowStock || s.Status == StockStatus.OutOfStock)
            .ToArray();
        return Task.FromResult(lowStock);
    }

    public Task<StockMovementDto[]> GetStockMovementsAsync(Guid itemId, DateTime fromDate, DateTime toDate)
    {
        var movements = _movements
            .Where(m => m.ItemId == itemId && m.MovementDate >= fromDate && m.MovementDate <= toDate)
            .ToArray();
        return Task.FromResult(movements);
    }

    public Task<StockAdjustmentDto> AdjustStockAsync(AdjustStockRequest request)
    {
        if (!_items.TryGetValue(request.ItemId, out var item))
        {
            throw new InvalidOperationException("Item not found");
        }

        if (!_stockLevels.TryGetValue(request.ItemId, out var stockLevel))
        {
            throw new InvalidOperationException("Stock level not found");
        }

        var adjustment = new StockAdjustmentDto
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            ItemCode = item.ItemCode,
            WarehouseId = request.WarehouseId,
            AdjustmentDate = DateTime.UtcNow,
            QuantityBefore = stockLevel.OnHand,
            AdjustmentQuantity = request.Quantity,
            QuantityAfter = stockLevel.OnHand + request.Quantity,
            Reason = request.Reason,
            Notes = request.Notes,
            AdjustedBy = "system"
        };

        // Update stock level
        var newOnHand = stockLevel.OnHand + request.Quantity;
        _stockLevels[request.ItemId] = stockLevel with
        {
            OnHand = newOnHand,
            Available = newOnHand - stockLevel.Reserved,
            Status = newOnHand <= _items[request.ItemId].ReorderPoint ? StockStatus.LowStock : StockStatus.InStock
        };

        // Record movement
        _movements.Add(new StockMovementDto
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            ItemCode = item.ItemCode,
            MovementDate = DateTime.UtcNow,
            Type = MovementType.Adjustment,
            Quantity = request.Quantity,
            BalanceBefore = adjustment.QuantityBefore,
            BalanceAfter = adjustment.QuantityAfter,
            WarehouseId = request.WarehouseId,
            Notes = request.Notes,
            CreatedBy = "system"
        });

        Console.WriteLine($"[Inventory] Adjusted stock: {item.ItemCode} by {request.Quantity}");
        return Task.FromResult(adjustment);
    }

    public Task<StockValuationDto> GetStockValuationAsync()
    {
        var valuationItems = _items.Values.Select(item =>
        {
            var stockLevel = _stockLevels.GetValueOrDefault(item.Id);
            var quantity = stockLevel?.OnHand ?? 0;
            return new StockValuationItemDto
            {
                ItemId = item.Id,
                ItemCode = item.ItemCode,
                ItemName = item.Name,
                Quantity = quantity,
                UnitCost = item.UnitCost,
                TotalValue = quantity * item.UnitCost
            };
        }).ToArray();

        var totalValue = valuationItems.Sum(v => v.TotalValue);
        foreach (var item in valuationItems)
        {
            item.ValuePercent = totalValue > 0 ? (item.TotalValue / totalValue) * 100 : 0;
        }

        return Task.FromResult(new StockValuationDto
        {
            AsOfDate = DateTime.UtcNow,
            TotalItems = valuationItems.Length,
            TotalUnits = valuationItems.Sum(v => v.Quantity),
            TotalValue = totalValue,
            Currency = "USD",
            Items = valuationItems.OrderByDescending(v => v.TotalValue).ToArray()
        });
    }

    public Task<StockValuationDto> GetStockValuationAsync(Guid warehouseId)
    {
        return GetStockValuationAsync(); // Simplified - single warehouse
    }
}
