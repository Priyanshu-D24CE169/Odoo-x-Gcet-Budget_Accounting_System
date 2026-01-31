using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;

namespace ShivFurnitureERP.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public DashboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> GetCustomerCountAsync(CancellationToken cancellationToken = default) =>
        _context.Customers.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> GetOrderCountAsync(CancellationToken cancellationToken = default) =>
        _context.Orders.AsNoTracking().CountAsync(cancellationToken);

    public async Task<decimal> GetCurrentMonthRevenueAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var startOfMonth = new DateTime(utcNow.Year, utcNow.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        var total = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= startOfMonth && o.OrderDate < endOfMonth)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken);

        return total ?? 0m;
    }

    public async Task<IReadOnlyList<RevenuePoint>> GetRecentRevenueAsync(int months, CancellationToken cancellationToken = default)
    {
        if (months <= 0)
        {
            return Array.Empty<RevenuePoint>();
        }

        var utcNow = DateTime.UtcNow;
        var startMonth = new DateTime(utcNow.Year, utcNow.Month, 1).AddMonths(-(months - 1));

        var revenueByMonth = await _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderDate >= startMonth)
            .GroupBy(order => new { order.OrderDate.Year, order.OrderDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Total = group.Sum(order => order.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var points = new List<RevenuePoint>(months);

        for (var i = 0; i < months; i++)
        {
            var cursor = startMonth.AddMonths(i);
            var match = revenueByMonth.FirstOrDefault(x => x.Year == cursor.Year && x.Month == cursor.Month);
            var amount = match?.Total ?? 0m;
            points.Add(new RevenuePoint(cursor.ToString("MMM yyyy"), amount));
        }

        return points;
    }
}
