namespace CareerPlatform.Api.Features.Payments.Dto;

/// <summary>
/// Body for <c>POST /api/v1/payments/create-order</c>. The caller supplies only the id of a stored
/// <c>SubscriptionPlan</c>. No client amount is accepted — price, currency, and plan identifiers
/// are sourced exclusively from the stored plan by the service.
/// </summary>
public sealed record CreateOrderRequest(int PlanId);
