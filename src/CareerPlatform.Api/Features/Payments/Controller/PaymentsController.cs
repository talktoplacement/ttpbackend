using CareerPlatform.Api.Features.Payments.Dto;
using CareerPlatform.Api.Features.Payments.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Payments.Controller;

/// <summary>
/// Payment-gateway surface for any authenticated user. Not admin-scoped — a signed-in student
/// creates and verifies their own order.
/// </summary>
[ApiController]
[Route("api/v1/payments")]   // canonical
[Produces("application/json")]
[Authorize]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;
    public PaymentsController(IPaymentService service) => _service = service;

    /// <summary>POST <c>/api/v1/payments/create-order</c> — open a Razorpay order for the caller's chosen plan.</summary>
    [HttpPost("create-order")]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest body, CancellationToken ct)
    {
        var result = await _service.CreateOrderAsync(body.PlanId, ct);
        return result.ToActionResult();
    }

    /// <summary>POST <c>/api/v1/payments/verify</c> — verify a Razorpay callback and activate the subscription.</summary>
    [HttpPost("verify")]
    public async Task<ActionResult<VerifyPaymentResponse>> Verify(
        [FromBody] VerifyPaymentRequest body, CancellationToken ct)
    {
        var result = await _service.VerifyAsync(body, ct);
        return result.ToActionResult();
    }

    /// <summary>GET <c>/api/v1/payments/orders/me</c> — the caller's own order history.</summary>
    [HttpGet("orders/me")]
    public async Task<ActionResult<IReadOnlyList<StudentOrderResponse>>> MyOrders(CancellationToken ct)
        => (await _service.ListMyOrdersAsync(ct)).ToActionResult();

    /// <summary>GET <c>/api/v1/payments/orders/{id}/me</c> — one order from the caller's history.</summary>
    [HttpGet("orders/{id:int}/me")]
    public async Task<ActionResult<StudentOrderResponse>> MyOrder(int id, CancellationToken ct)
        => (await _service.GetMyOrderAsync(id, ct)).ToActionResult();
}
