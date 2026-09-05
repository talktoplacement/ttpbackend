namespace CareerPlatform.Api.Infrastructure;

/// <summary>
/// A payment-gateway abstraction. Order creation is asynchronous and returns a
/// <see cref="Result{T}"/> so gateway failures surface as domain failures rather than
/// exceptions. Signature verification is pure and side-effect free so it can run before any
/// persistence (Req 17.4, 22.1). Adding a second adapter is a registration-only change
/// (Req 17.5).
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Creates an order at the gateway for <paramref name="amount"/> tagged with
    /// <paramref name="receiptId"/>, returning the gateway order id on success or a failure
    /// <see cref="Result{T}"/> when the gateway rejects/errors.
    /// </summary>
    Task<Result<string>> CreateOrderAsync(decimal amount, string receiptId, CancellationToken ct);

    /// <summary>
    /// Verifies the signature on a payment callback. Never throws: a malformed or invalid
    /// callback yields <see cref="SignatureVerificationResult.IsValid"/> = <c>false</c> with a
    /// reason (Req 22.1, 22.3). Callers MUST verify before any persistence (Req 22.1).
    /// </summary>
    SignatureVerificationResult VerifySignature(PaymentCallback callback);
}

/// <summary>
/// The outcome of verifying a payment-callback signature.
/// </summary>
/// <param name="IsValid">Whether the signature is valid for the callback payload.</param>
/// <param name="FailureReason">A human-readable reason when invalid; <c>null</c> when valid.</param>
public sealed record SignatureVerificationResult(bool IsValid, string? FailureReason);

/// <summary>
/// The fields returned by the gateway when a payment completes, used for signature verification.
/// </summary>
/// <param name="OrderId">The gateway order id (e.g. <c>razorpay_order_id</c>).</param>
/// <param name="PaymentId">The gateway payment id (e.g. <c>razorpay_payment_id</c>).</param>
/// <param name="Signature">The gateway-supplied signature (e.g. <c>razorpay_signature</c>).</param>
public sealed record PaymentCallback(string OrderId, string PaymentId, string Signature);
