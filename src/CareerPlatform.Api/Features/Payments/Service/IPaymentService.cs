using CareerPlatform.Api.Features.Payments.Dto;

namespace CareerPlatform.Api.Features.Payments.Service;

/// <summary>
/// Public contract for the payment-gateway workflow: open a gateway order and verify the callback
/// that provisions the caller's subscription. Consumed by <see cref="Controller.PaymentsController"/>.
/// </summary>
public interface IPaymentService
{
    Task<Result<CreateOrderResponse>> CreateOrderAsync(int planId, CancellationToken ct);

    Task<Result<VerifyPaymentResponse>> VerifyAsync(VerifyPaymentRequest request, CancellationToken ct);

    /// <summary>The authenticated caller's own completed/failed order history, newest first.</summary>
    Task<Result<IReadOnlyList<StudentOrderResponse>>> ListMyOrdersAsync(CancellationToken ct);

    /// <summary>A single order from the caller's own history (404 if it is not theirs).</summary>
    Task<Result<StudentOrderResponse>> GetMyOrderAsync(int id, CancellationToken ct);
}
