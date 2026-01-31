using ShivFurnitureERP.ViewModels.Payments;

namespace ShivFurnitureERP.Services;

public interface IPaymentGatewaySimulator
{
    Task<PaymentGatewayResult> CapturePaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default);
}
