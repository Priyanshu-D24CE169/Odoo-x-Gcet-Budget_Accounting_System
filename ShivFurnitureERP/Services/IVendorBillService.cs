using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface IVendorBillService
{
    Task<IReadOnlyList<VendorBill>> GetBillsAsync(string? search, VendorBillStatus? status, VendorBillPaymentStatus? paymentStatus, CancellationToken cancellationToken = default);
    Task<VendorBill?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<VendorBill> CreateDraftFromPurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken = default);
    Task UpdateAsync(VendorBill bill, CancellationToken cancellationToken = default);
    Task ConfirmAsync(int billId, CancellationToken cancellationToken = default);
    Task CancelAsync(int billId, CancellationToken cancellationToken = default);
}
