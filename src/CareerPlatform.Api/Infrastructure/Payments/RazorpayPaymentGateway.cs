using System.Security.Cryptography;
using System.Text;
using CareerPlatform.Api.Configuration;
using Razorpay.Api;

namespace CareerPlatform.Api.Infrastructure.Payments;

/// <summary>
/// <see cref="IPaymentGateway"/> adapter over Razorpay. Order creation wraps the Razorpay SDK
/// and surfaces SDK failures as a failure <see cref="Result{T}"/> rather than throwing
/// (Req 17.4). Signature verification is implemented directly as the HMAC-SHA256 check that
/// Razorpay's <c>Utils.verifyPaymentSignature</c> performs, so it is deterministic, testable,
/// and never throws on an invalid signature (Req 22.1, 22.3).
/// </summary>
public sealed class RazorpayPaymentGateway(IOptions<RazorpayOptions> options) : IPaymentGateway
{
    private readonly RazorpayOptions _options = options.Value;

    /// <inheritdoc />
    public Task<Result<string>> CreateOrderAsync(
        decimal amount, string receiptId, CancellationToken ct)
    {
        try
        {
            var client = new RazorpayClient(_options.KeyId, _options.KeySecret);

            var orderOptions = new Dictionary<string, object>
            {
                ["amount"] = (int)(amount * 100), // smallest currency unit (paise)
                ["currency"] = "INR",
                ["receipt"] = receiptId,
                ["payment_capture"] = 1,
            };

            var order = client.Order.Create(orderOptions);
            var orderId = order["id"]?.ToString();

            return Task.FromResult(string.IsNullOrEmpty(orderId)
                ? Result.Failure<string>(Error.Failure(
                    "Payment.OrderCreationFailed",
                    "The payment gateway did not return an order id."))
                : Result.Success(orderId));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<string>(Error.Failure(
                "Payment.OrderCreationFailed",
                $"The payment gateway failed to create an order: {ex.Message}")));
        }
    }

    /// <inheritdoc />
    public SignatureVerificationResult VerifySignature(PaymentCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        // Reject malformed payloads up front without throwing (Req 22.3).
        if (string.IsNullOrEmpty(callback.OrderId))
        {
            return new SignatureVerificationResult(false, "Order id is required.");
        }

        if (string.IsNullOrEmpty(callback.PaymentId))
        {
            return new SignatureVerificationResult(false, "Payment id is required.");
        }

        if (string.IsNullOrEmpty(callback.Signature))
        {
            return new SignatureVerificationResult(false, "Signature is required.");
        }

        // Razorpay signs "{order_id}|{payment_id}" with HMAC-SHA256 keyed by the key secret,
        // hex-encoded lowercase. Compare in constant time (Req 22.1).
        var expected = ComputeHmacSha256Hex(
            $"{callback.OrderId}|{callback.PaymentId}", _options.KeySecret);

        var provided = callback.Signature;
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));

        return matches
            ? new SignatureVerificationResult(true, null)
            : new SignatureVerificationResult(false, "Signature does not match the payload.");
    }

    private static string ComputeHmacSha256Hex(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
