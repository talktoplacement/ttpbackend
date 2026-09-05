namespace CareerPlatform.Api.Features.Payments.Dto;

/// <summary>
/// Gateway-order details returned to the client so it can open the Razorpay checkout.
/// <see cref="Amount"/> and <see cref="Currency"/> come exclusively from the stored plan.
/// </summary>
public sealed record CreateOrderResponse(
    string OrderId,
    decimal Amount,
    string Currency,
    string KeyId,
    int PlanId,
    string PlanCode,
    string PlanName);
