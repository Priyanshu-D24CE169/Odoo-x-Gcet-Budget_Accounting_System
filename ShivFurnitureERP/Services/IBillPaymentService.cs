using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface IBillPaymentService
{
    Task<IReadOnlyList<BillPayment>> GetPaymentsForBillAsync(int vendorBillId, CancellationToken cancellationToken = default);
    Task<BillPayment> CreateAsync(BillPayment payment, CancellationToken cancellationToken = default);
    Task<string> PeekNextPaymentNumberAsync(CancellationToken cancellationToken = default);
}
