namespace ShivFurnitureERP.ViewModels.Payments;

public class PaymentGatewayResult
{
    public bool Succeeded { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
