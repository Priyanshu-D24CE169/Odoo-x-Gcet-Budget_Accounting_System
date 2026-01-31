using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface ISalesOrderService
{
    Task<IReadOnlyList<SalesOrder>> GetOrdersAsync(string? search, SalesOrderStatus? status, CancellationToken cancellationToken = default);
    Task<SalesOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SalesOrder> CreateAsync(SalesOrder order, CancellationToken cancellationToken = default);
    Task UpdateAsync(SalesOrder order, CancellationToken cancellationToken = default);
    Task ConfirmAsync(int orderId, CancellationToken cancellationToken = default);
    Task CancelAsync(int orderId, CancellationToken cancellationToken = default);
}
