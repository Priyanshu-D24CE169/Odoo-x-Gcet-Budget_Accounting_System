using System;
using ShivFurnitureERP.ViewModels.Payments;

namespace ShivFurnitureERP.Services;

public class PaymentGatewaySimulator : IPaymentGatewaySimulator
{
    private static readonly TimeSpan SimulatedDelay = TimeSpan.FromSeconds(1);

    public async Task<PaymentGatewayResult> CapturePaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedDelay, cancellationToken);

        return new PaymentGatewayResult
        {
            Succeeded = true,
            TransactionId = $"SIM-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(1000, 9999)}",
            Message = $"Payment of {request.Amount:C} for {request.InvoiceNumber} authorized via {request.Mode}."
        };
    }
}
