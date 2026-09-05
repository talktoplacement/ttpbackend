namespace CareerPlatform.Api.Features.Payments.Dto;

/// <summary>
/// Body for <c>POST /api/v1/payments/verify</c>. Carries ONLY the three fields Razorpay hands back
/// to the browser.
///
/// The plan is deliberately NOT part of this contract. It is resolved server-side from the
/// <c>Order</c> row that was persisted when the checkout was created, keyed by
/// <see cref="RazorpayOrderId"/> and scoped to the calling user.
///
/// This matters: the Razorpay signature only covers <c>{order_id}|{payment_id}</c> — it says nothing
/// about the amount or the plan. When the plan came from the request body, a caller could pay for the
/// cheapest tier, receive a genuine signature, and then submit that authentic triple alongside a
/// premium plan id to be provisioned on a plan they never paid for (and the ledger would record
/// revenue that was never collected). Binding the plan to the stored order closes that hole.
/// </summary>
public sealed record VerifyPaymentRequest(
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature);
