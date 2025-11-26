using System.Collections.Concurrent;
using FinanceSupplyChain.Contracts.SupplyChain;

namespace Procurement.Node;

public class ProcurementService : IProcurementService
{
    private readonly ConcurrentDictionary<Guid, SupplierDto> _suppliers = new();
    private readonly ConcurrentDictionary<Guid, PurchaseOrderDto> _orders = new();
    private readonly ConcurrentDictionary<Guid, PurchaseRequisitionDto> _requisitions = new();
    private int _poCounter = 4000;

    public ProcurementService()
    {
        SeedData();
    }

    private void SeedData()
    {
        var suppliers = new[]
        {
            new SupplierDto 
            { 
                Id = Guid.NewGuid(), Code = "SUP001", Name = "Global Manufacturing Co", 
                ContactName = "Mike Chen", Email = "mike@globalmanufacturing.com", Phone = "555-2001",
                Address = new AddressDto { Street = "1000 Industrial Blvd", City = "Manufacturing City", State = "CA", PostalCode = "90001", Country = "USA" },
                PaymentTerms = "Net 30", CreditLimit = 100000m, IsActive = true, Rating = SupplierRating.Excellent
            },
            new SupplierDto 
            { 
                Id = Guid.NewGuid(), Code = "SUP002", Name = "Quality Components Ltd", 
                ContactName = "Sarah Lee", Email = "sarah@qualitycomponents.com", Phone = "555-2002",
                Address = new AddressDto { Street = "500 Component Way", City = "Tech City", State = "TX", PostalCode = "75001", Country = "USA" },
                PaymentTerms = "Net 45", CreditLimit = 75000m, IsActive = true, Rating = SupplierRating.Good
            },
            new SupplierDto 
            { 
                Id = Guid.NewGuid(), Code = "SUP003", Name = "Raw Materials Inc", 
                ContactName = "David Brown", Email = "david@rawmaterials.com", Phone = "555-2003",
                Address = new AddressDto { Street = "200 Material St", City = "Resource Town", State = "OH", PostalCode = "44001", Country = "USA" },
                PaymentTerms = "Net 15", CreditLimit = 50000m, IsActive = true, Rating = SupplierRating.Acceptable
            },
        };

        foreach (var supplier in suppliers)
        {
            _suppliers[supplier.Id] = supplier;
        }

        // Create sample PO
        var s1 = suppliers[0];
        var po = new PurchaseOrderDto
        {
            Id = Guid.NewGuid(),
            PoNumber = $"PO-{++_poCounter}",
            SupplierId = s1.Id,
            SupplierName = s1.Name,
            OrderDate = DateTime.Today.AddDays(-5),
            ExpectedDeliveryDate = DateTime.Today.AddDays(10),
            Status = PurchaseOrderStatus.Sent,
            Lines = new[]
            {
                new PurchaseOrderLineDto { LineNumber = 1, ItemId = Guid.NewGuid(), ItemCode = "SKU002", Description = "Widget B", Quantity = 200, Unit = "EA", UnitPrice = 25.00m, LineTotal = 5000.00m, ReceivedQuantity = 0, Status = PoLineStatus.Open },
            },
            Subtotal = 5000.00m,
            Tax = 400.00m,
            Total = 5400.00m,
            Currency = "USD",
            ApprovedBy = "John Manager",
            ApprovedDate = DateTime.Today.AddDays(-4)
        };
        _orders[po.Id] = po;

        // Create sample requisition
        var req = new PurchaseRequisitionDto
        {
            Id = Guid.NewGuid(),
            RequisitionNumber = "REQ-001",
            RequestedBy = "Jane Engineer",
            RequestDate = DateTime.Today.AddDays(-2),
            NeededByDate = DateTime.Today.AddDays(14),
            Status = RequisitionStatus.Submitted,
            Lines = new[]
            {
                new RequisitionLineDto { LineNumber = 1, ItemId = Guid.NewGuid(), ItemDescription = "Electronic Component", Quantity = 100, Unit = "EA", EstimatedPrice = 5.00m }
            },
            Justification = "Required for Q2 production"
        };
        _requisitions[req.Id] = req;
    }

    public Task<PurchaseOrderDto[]> GetPurchaseOrdersAsync()
    {
        return Task.FromResult(_orders.Values.OrderByDescending(o => o.OrderDate).ToArray());
    }

    public Task<PurchaseOrderDto> GetPurchaseOrderAsync(Guid poId)
    {
        _orders.TryGetValue(poId, out var po);
        return Task.FromResult(po!);
    }

    public Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request)
    {
        if (!_suppliers.TryGetValue(request.SupplierId, out var supplier))
        {
            throw new InvalidOperationException("Supplier not found");
        }

        var lines = request.Lines.Select((l, i) => new PurchaseOrderLineDto
        {
            LineNumber = i + 1,
            ItemId = l.ItemId,
            ItemCode = $"ITEM-{l.ItemId.ToString()[..8]}",
            Description = "Item description",
            Quantity = l.Quantity,
            Unit = "EA",
            UnitPrice = l.UnitPrice,
            LineTotal = l.Quantity * l.UnitPrice,
            ReceivedQuantity = 0,
            Status = PoLineStatus.Open
        }).ToArray();

        var subtotal = lines.Sum(l => l.LineTotal);
        var po = new PurchaseOrderDto
        {
            Id = Guid.NewGuid(),
            PoNumber = $"PO-{++_poCounter}",
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            OrderDate = DateTime.Today,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Status = PurchaseOrderStatus.Draft,
            Lines = lines,
            Subtotal = subtotal,
            Tax = subtotal * 0.08m,
            Total = subtotal * 1.08m,
            Currency = "USD",
            Notes = request.Notes
        };

        _orders[po.Id] = po;
        Console.WriteLine($"[Procurement] Created PO: {po.PoNumber} for {supplier.Name}");
        return Task.FromResult(po);
    }

    public Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(Guid poId, string approverNotes)
    {
        if (!_orders.TryGetValue(poId, out var po))
        {
            throw new InvalidOperationException("Purchase order not found");
        }

        var updated = po with
        {
            Status = PurchaseOrderStatus.Approved,
            ApprovedBy = "Approver",
            ApprovedDate = DateTime.UtcNow,
            Notes = $"{po.Notes}\nApproval: {approverNotes}"
        };
        _orders[poId] = updated;

        Console.WriteLine($"[Procurement] Approved PO: {po.PoNumber}");
        return Task.FromResult(updated);
    }

    public Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(Guid poId, ReceiveItemsRequest[] items)
    {
        if (!_orders.TryGetValue(poId, out var po))
        {
            throw new InvalidOperationException("Purchase order not found");
        }

        var updatedLines = po.Lines.Select(line =>
        {
            var received = items.FirstOrDefault(i => i.LineNumber == line.LineNumber);
            if (received != null)
            {
                var newReceived = line.ReceivedQuantity + received.Quantity;
                var newStatus = newReceived >= line.Quantity ? PoLineStatus.Received : PoLineStatus.PartiallyReceived;
                return line with { ReceivedQuantity = newReceived, Status = newStatus };
            }
            return line;
        }).ToArray();

        var allReceived = updatedLines.All(l => l.Status == PoLineStatus.Received);
        var anyReceived = updatedLines.Any(l => l.Status == PoLineStatus.Received || l.Status == PoLineStatus.PartiallyReceived);

        var updated = po with
        {
            Lines = updatedLines,
            Status = allReceived ? PurchaseOrderStatus.Received : (anyReceived ? PurchaseOrderStatus.PartiallyReceived : po.Status)
        };
        _orders[poId] = updated;

        Console.WriteLine($"[Procurement] Received items for PO: {po.PoNumber}");
        return Task.FromResult(updated);
    }

    public Task<SupplierDto[]> GetSuppliersAsync()
    {
        return Task.FromResult(_suppliers.Values.ToArray());
    }

    public Task<SupplierDto> GetSupplierAsync(Guid supplierId)
    {
        _suppliers.TryGetValue(supplierId, out var supplier);
        return Task.FromResult(supplier!);
    }

    public Task<SupplierPerformanceDto> GetSupplierPerformanceAsync(Guid supplierId)
    {
        if (!_suppliers.TryGetValue(supplierId, out var supplier))
        {
            throw new InvalidOperationException("Supplier not found");
        }

        // Mock performance data
        return Task.FromResult(new SupplierPerformanceDto
        {
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            TotalOrders = 25,
            OnTimeDeliveries = 23,
            OnTimeDeliveryPercent = 92m,
            QualityAcceptances = 24,
            QualityAcceptancePercent = 96m,
            AverageLeadTimeDays = 12,
            TotalSpend = 150000m,
            OverallRating = supplier.Rating
        });
    }

    public Task<PurchaseRequisitionDto[]> GetPendingRequisitionsAsync()
    {
        var pending = _requisitions.Values
            .Where(r => r.Status == RequisitionStatus.Submitted || r.Status == RequisitionStatus.Approved)
            .ToArray();
        return Task.FromResult(pending);
    }
}
