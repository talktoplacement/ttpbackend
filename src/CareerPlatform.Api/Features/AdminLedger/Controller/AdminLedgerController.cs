using CareerPlatform.Api.Features.AdminLedger.Dto;
using CareerPlatform.Api.Features.AdminLedger.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.AdminLedger.Controller;

[ApiController]
[Route("api/v1/admin")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminLedgerController : ControllerBase
{
    private readonly IAdminLedgerService _service;
    public AdminLedgerController(IAdminLedgerService service) => _service = service;

    /// <summary>GET /orders?status=completed|pending|refunded|failed — global order ledger.</summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<OrderInvoiceResponse>>> ListOrders(
        [FromQuery] string? status, CancellationToken ct)
        => (await _service.ListOrdersAsync(status, ct)).ToActionResult();

    /// <summary>GET /orders/{id} — a single order invoice for the detail view.</summary>
    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderInvoiceResponse>> GetOrder(int id, CancellationToken ct)
        => (await _service.GetOrderAsync(id, ct)).ToActionResult();

    /// <summary>GET /payments — every captured Razorpay transaction, newest first.</summary>
    [HttpGet("payments")]
    public async Task<ActionResult<IReadOnlyList<PaymentLedgerRow>>> ListPayments(CancellationToken ct)
        => (await _service.ListPaymentsAsync(ct)).ToActionResult();

    /// <summary>GET /audit-logs?action=&amp;actor= — filterable privileged-action trail.</summary>
    [HttpGet("audit-logs")]
    public async Task<ActionResult<IReadOnlyList<AdminAuditLogResponse>>> ListAuditLogs(
        [FromQuery] string? action, [FromQuery(Name = "actor")] string? actorUserId,
        CancellationToken ct)
        => (await _service.ListAuditLogsAsync(action, actorUserId, ct)).ToActionResult();
}
