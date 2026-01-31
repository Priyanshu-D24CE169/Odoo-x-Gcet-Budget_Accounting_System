using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAnalyticalRuleEngine _ruleEngine;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(
        ApplicationDbContext dbContext,
        IAnalyticalRuleEngine ruleEngine,
        ILogger<PurchaseOrderService> logger)
    {
        _dbContext = dbContext;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetOrdersAsync(string? search, PurchaseOrderStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PurchaseOrders
            .Include(po => po.Vendor)
            .Include(po => po.Lines)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(po => po.PONumber.Contains(term) || (po.Vendor != null && po.Vendor.Name.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(po => po.Status == status.Value);
        }

        return await query
            .OrderByDescending(po => po.PurchaseOrderId)
            .ToListAsync(cancellationToken);
    }

    public Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PurchaseOrders
            .Include(po => po.Vendor)
            .Include(po => po.Lines)
                .ThenInclude(l => l.Product)
            .Include(po => po.Lines)
                .ThenInclude(l => l.AnalyticalAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);
    }

    public async Task<PurchaseOrder> CreateAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.PONumber))
        {
            order.PONumber = await GenerateNextPONumberAsync(cancellationToken);
        }

        await ApplyLineDefaultsAsync(order, cancellationToken);

        _dbContext.PurchaseOrders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task UpdateAsync(PurchaseOrder updatedOrder, CancellationToken cancellationToken = default)
    {
        var existingOrder = await _dbContext.PurchaseOrders
            .Include(po => po.Lines)
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == updatedOrder.PurchaseOrderId, cancellationToken);

        if (existingOrder is null)
        {
            throw new InvalidOperationException($"Purchase Order {updatedOrder.PurchaseOrderId} not found.");
        }

        if (existingOrder.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft purchase orders can be edited.");
        }

        existingOrder.VendorId = updatedOrder.VendorId;
        existingOrder.Reference = updatedOrder.Reference;
        existingOrder.PODate = updatedOrder.PODate;

        _dbContext.PurchaseOrderLines.RemoveRange(existingOrder.Lines);
        existingOrder.Lines.Clear();

        foreach (var line in updatedOrder.Lines)
        {
            existingOrder.Lines.Add(line);
        }

        await ApplyLineDefaultsAsync(existingOrder, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(po => po.PurchaseOrderId == orderId, cancellationToken);
        if (order is null)
        {
            throw new InvalidOperationException($"Purchase Order {orderId} not found.");
        }

        if (order.Status == PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled purchase orders cannot be confirmed.");
        }

        if (order.Status == PurchaseOrderStatus.Confirmed)
        {
            return;
        }

        order.Status = PurchaseOrderStatus.Confirmed;
        order.ConfirmedOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(po => po.PurchaseOrderId == orderId, cancellationToken);
        if (order is null)
        {
            throw new InvalidOperationException($"Purchase Order {orderId} not found.");
        }

        if (order.Status == PurchaseOrderStatus.Cancelled)
        {
            return;
        }

        order.Status = PurchaseOrderStatus.Cancelled;
        order.CancelledOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyLineDefaultsAsync(PurchaseOrder order, CancellationToken cancellationToken)
    {
        var vendor = await _dbContext.Contacts
            .Include(c => c.ContactTags)
            .FirstOrDefaultAsync(c => c.ContactId == order.VendorId, cancellationToken);

        var partnerTags = vendor?.ContactTags.Select(ct => ct.TagId).ToArray() ?? Array.Empty<int>();

        var productIds = order.Lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToArray();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.ProductId))
            .Include(p => p.Category)
            .ToDictionaryAsync(p => p.ProductId, cancellationToken);

        foreach (var line in order.Lines)
        {
            line.Total = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);

            if (line.AnalyticalAccountId.HasValue || !line.ProductId.HasValue)
            {
                continue;
            }

            products.TryGetValue(line.ProductId.Value, out var product);
            var request = new AnalyticalAssignmentRequest(
                order.VendorId,
                partnerTags,
                line.ProductId,
                product?.ProductCategoryId,
                AnalyticalAssignmentSource.PurchaseOrder);

            var result = await _ruleEngine.ResolveAsync(request, cancellationToken);
            if (result is not null)
            {
                line.AnalyticalAccountId = result.AnalyticalAccountId;
            }
        }
    }

    private async Task<string> GenerateNextPONumberAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var prefix = $"PO-{now:yyyyMM}-";
        var lastNumber = await _dbContext.PurchaseOrders
            .Where(po => po.PONumber.StartsWith(prefix))
            .OrderByDescending(po => po.PONumber)
            .Select(po => po.PONumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (!string.IsNullOrWhiteSpace(lastNumber) && lastNumber.Length >= prefix.Length)
        {
            var numericPart = lastNumber[prefix.Length..];
            if (int.TryParse(numericPart, out var parsed))
            {
                sequence = parsed + 1;
            }
        }

        return prefix + sequence.ToString("D4");
    }
}
