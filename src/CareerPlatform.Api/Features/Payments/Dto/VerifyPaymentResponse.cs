namespace CareerPlatform.Api.Features.Payments.Dto;

/// <summary>Confirmation returned after a payment callback is verified and the subscription provisioned.</summary>
public sealed record VerifyPaymentResponse(bool Success, string Message, string ActivePlan);
