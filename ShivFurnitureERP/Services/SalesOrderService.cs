using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public class SalesOrderService : ISalesOrderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SalesOrderService> _logger;

    public SalesOrderService(ApplicationDbContext dbContext, ILogger<SalesOrderService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SalesOrder>> GetOrdersAsync(string? search, SalesOrderStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SalesOrders
            .Include(order => order.Customer)
            .OrderByDescending(order => order.SalesOrderId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(order => order.SONumber.Contains(term) || (order.Customer != null && order.Customer.Name.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(order => order.Status == status.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<SalesOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.SalesOrders
            .Include(order => order.Customer)
            .Include(order => order.Lines)
                .ThenInclude(line => line.Product)
            .Include(order => order.Lines)
                .ThenInclude(line => line.AnalyticalAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(order => order.SalesOrderId == id, cancellationToken);
    }

    public async Task<SalesOrder> CreateAsync(SalesOrder order, CancellationToken cancellationToken = default)
    {
        order.SONumber = await GenerateNextNumberAsync(cancellationToken);
        order.CreatedOn = DateTime.UtcNow;
        await FillLineTotalsAsync(order);

        _dbContext.SalesOrders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task UpdateAsync(SalesOrder order, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.SalesOrderId == order.SalesOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Sales Order {order.SalesOrderId} not found.");

        if (existing.Status != SalesOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft sales orders can be edited.");
        }

        existing.CustomerId = order.CustomerId;
        existing.SODate = order.SODate;
        existing.Reference = order.Reference;

        _dbContext.SalesOrderLines.RemoveRange(existing.Lines);
        existing.Lines.Clear();

        foreach (var line in order.Lines)
        {
            line.SalesOrderId = existing.SalesOrderId;
            existing.Lines.Add(line);
        }

        await FillLineTotalsAsync(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.SalesOrders.FirstOrDefaultAsync(o => o.SalesOrderId == orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Sales Order {orderId} not found.");

        if (order.Status == SalesOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot be confirmed.");
        }

        if (order.Status == SalesOrderStatus.Confirmed)
        {
            return;
        }

        order.Status = SalesOrderStatus.Confirmed;
        order.ConfirmedOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.SalesOrders.FirstOrDefaultAsync(o => o.SalesOrderId == orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Sales Order {orderId} not found.");

        if (order.Status == SalesOrderStatus.Cancelled)
        {
            return;
        }

        order.Status = SalesOrderStatus.Cancelled;
        order.CancelledOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateNextNumberAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var prefix = $"SO-{now:yyyyMM}-";
        var last = await _dbContext.SalesOrders
            .Where(order => order.SONumber.StartsWith(prefix))
            .OrderByDescending(order => order.SONumber)
            .Select(order => order.SONumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (!string.IsNullOrEmpty(last))
        {
            var numeric = last[prefix.Length..];
            if (int.TryParse(numeric, out var parsed))
            {
                sequence = parsed + 1;
            }
        }

        return prefix + sequence.ToString("D4");
    }

    private Task FillLineTotalsAsync(SalesOrder order)
    {
        foreach (var line in order.Lines)
        {
            line.Total = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
        }

        return Task.CompletedTask;
    }
}
