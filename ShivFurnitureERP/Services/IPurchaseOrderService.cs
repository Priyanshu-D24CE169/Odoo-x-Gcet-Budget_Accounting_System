using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrder>> GetOrdersAsync(string? search, PurchaseOrderStatus? status, CancellationToken cancellationToken = default);
    Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PurchaseOrder> CreateAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
    Task ConfirmAsync(int orderId, CancellationToken cancellationToken = default);
    Task CancelAsync(int orderId, CancellationToken cancellationToken = default);
}
