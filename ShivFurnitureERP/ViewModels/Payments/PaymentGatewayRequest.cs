using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.ViewModels.Payments;

public class PaymentGatewayRequest
{
    public required string InvoiceNumber { get; init; }
    public required string CustomerName { get; init; }
    public required decimal Amount { get; init; }
    public required PaymentMode Mode { get; init; }
}
