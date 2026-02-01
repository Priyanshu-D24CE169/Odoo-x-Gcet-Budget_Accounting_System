using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Razorpay.Api;
using ShivFurnitureERP.Options;
using ShivFurnitureERP.ViewModels.Payments;

namespace ShivFurnitureERP.Services;

/// <summary>
/// Razorpay payment gateway integration service
/// </summary>
public class RazorpayPaymentService : IPaymentGatewaySimulator
{
    private readonly RazorpayOptions _options;
    private readonly ILogger<RazorpayPaymentService> _logger;
    private readonly RazorpayClient _razorpayClient;

    public RazorpayPaymentService(IOptions<RazorpayOptions> options, ILogger<RazorpayPaymentService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.KeyId) || string.IsNullOrWhiteSpace(_options.KeySecret))
        {
            _logger.LogWarning("Razorpay credentials are missing. Configure Razorpay:KeyId and Razorpay:KeySecret in appsettings.json.");
        }

        _razorpayClient = new RazorpayClient(_options.KeyId, _options.KeySecret);
    }

    /// <summary>
    /// Creates a Razorpay Order for invoice payment
    /// </summary>
    public async Task<RazorpayOrderResponse> CreateOrderAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.KeyId) || string.IsNullOrWhiteSpace(_options.KeySecret))
            {
                return new RazorpayOrderResponse
                {
                    Success = false,
                    ErrorMessage = "Razorpay credentials not configured."
                };
            }

            // Amount must be in paise (multiply by 100)
            var amountInPaise = (int)(request.Amount * 100);

            var orderOptions = new Dictionary<string, object>
            {
                { "amount", amountInPaise },
                { "currency", "INR" },
                { "receipt", $"rcpt_{request.InvoiceNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}" },
                { "notes", new Dictionary<string, string>
                    {
                        { "invoice_number", request.InvoiceNumber },
                        { "customer_name", request.CustomerName },
                        { "payment_mode", request.Mode.ToString() }
                    }
                }
            };

            var order = await Task.Run(() => _razorpayClient.Order.Create(orderOptions), cancellationToken);

            var orderId = order["id"]?.ToString() ?? string.Empty;
            _logger.LogInformation($"Razorpay order created: {orderId} for invoice {request.InvoiceNumber}, amount: {request.Amount}");

            return new RazorpayOrderResponse
            {
                Success = true,
                OrderId = orderId,
                Amount = request.Amount,
                Currency = "INR",
                KeyId = _options.KeyId,
                CompanyName = _options.CompanyName,
                CompanyLogo = _options.CompanyLogo,
                ThemeColor = _options.ThemeColor,
                InvoiceNumber = request.InvoiceNumber,
                CustomerName = request.CustomerName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Razorpay order for invoice {InvoiceNumber}", request.InvoiceNumber);
            return new RazorpayOrderResponse
            {
                Success = false,
                ErrorMessage = "Failed to initiate payment. Please try again later."
            };
        }
    }

    /// <summary>
    /// Verifies Razorpay payment signature
    /// </summary>
    public bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
    {
        try
        {
            var payload = $"{razorpayOrderId}|{razorpayPaymentId}";
            var secret = _options.KeySecret;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = BitConverter.ToString(computedHash).Replace("-", "").ToLower();

            var isValid = computedSignature == razorpaySignature.ToLower();

            if (isValid)
            {
                _logger.LogInformation("Razorpay signature verified successfully for payment {PaymentId}", razorpayPaymentId);
            }
            else
            {
                _logger.LogWarning("Razorpay signature verification failed for payment {PaymentId}", razorpayPaymentId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Razorpay signature for payment {PaymentId}", razorpayPaymentId);
            return false;
        }
    }

    /// <summary>
    /// Captures payment after signature verification (implements IPaymentGatewaySimulator for compatibility)
    /// </summary>
    public async Task<PaymentGatewayResult> CapturePaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken = default)
    {
        // This method is used for simulator compatibility
        // Real Razorpay flow uses CreateOrderAsync ? VerifyPaymentSignature ? FetchPaymentDetails
        _logger.LogWarning("CapturePaymentAsync called - use CreateOrderAsync for Razorpay integration");

        return new PaymentGatewayResult
        {
            Succeeded = false,
            Message = "Use Razorpay order creation flow instead of direct capture.",
            TransactionId = string.Empty
        };
    }

    /// <summary>
    /// Fetches payment details from Razorpay
    /// </summary>
    public async Task<PaymentGatewayResult> FetchPaymentDetailsAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await Task.Run(() => _razorpayClient.Payment.Fetch(paymentId), cancellationToken);

            var status = payment["status"]?.ToString() ?? "unknown";
            var method = payment["method"]?.ToString() ?? "unknown";
            var succeeded = status == "captured" || status == "authorized";

            _logger.LogInformation($"Fetched Razorpay payment {paymentId}, status: {status}");

            return new PaymentGatewayResult
            {
                Succeeded = succeeded,
                TransactionId = paymentId,
                Message = $"Payment {status} - {method}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Razorpay payment details for {PaymentId}", paymentId);
            return new PaymentGatewayResult
            {
                Succeeded = false,
                TransactionId = paymentId,
                Message = "Failed to verify payment details."
            };
        }
    }
}

/// <summary>
/// Response model for Razorpay order creation
/// </summary>
public class RazorpayOrderResponse
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string KeyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyLogo { get; set; } = string.Empty;
    public string ThemeColor { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
