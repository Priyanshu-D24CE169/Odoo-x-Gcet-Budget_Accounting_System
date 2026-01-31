using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface IBudgetWarningService
{
    Task<IReadOnlyDictionary<int, BudgetWarningResult>> EvaluateAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, BudgetWarningResult>> EvaluateAsync(VendorBill bill, CancellationToken cancellationToken = default);
}
