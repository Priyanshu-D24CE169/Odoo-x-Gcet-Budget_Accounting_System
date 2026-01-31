namespace ShivFurnitureERP.Services;

public record BudgetWarningResult(bool IsExceeded, decimal Limit, decimal ProjectedAmount, string Message);
