namespace ShivFurnitureERP.Repositories;

public interface IDashboardRepository
{
    Task<int> GetCustomerCountAsync(CancellationToken cancellationToken = default);

    Task<int> GetOrderCountAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetCurrentMonthRevenueAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RevenuePoint>> GetRecentRevenueAsync(int months, CancellationToken cancellationToken = default);
}

public record RevenuePoint(string Label, decimal Amount);
