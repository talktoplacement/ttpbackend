using CareerPlatform.Api.Features.AdminLedger.Domain;
using CareerPlatform.Api.Features.AdminLedger.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.AdminLedger.Service;

internal sealed class AdminLedgerService : IAdminLedgerService
{
    private readonly AppDbContext _db;
    public AdminLedgerService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<OrderInvoiceResponse>>> ListOrdersAsync(
        string? status, CancellationToken ct)
    {
        var q = _db.OrderInvoices.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            // Reject an unrecognised filter rather than silently returning zero rows — an admin
            // typo would otherwise look identical to "no orders in that state".
            if (!OrderInvoiceStatus.IsValid(status))
            {
                return Result.Failure<IReadOnlyList<OrderInvoiceResponse>>(Error.Validation(
                    "OrderInvoice.InvalidStatus",
                    $"Status must be one of: {string.Join(", ", OrderInvoiceStatus.All)}."));
            }
            var s = status.Trim().ToLowerInvariant();
            q = q.Where(o => o.Status == s);
        }
        var rows = await q
            .OrderByDescending(o => o.PurchasedAtUtc)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<OrderInvoiceResponse>)rows.Select(OrderInvoiceResponse.From).ToList());
    }

    public async Task<Result<OrderInvoiceResponse>> GetOrderAsync(int id, CancellationToken ct)
    {
        var order = await _db.OrderInvoices.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return Result.Failure<OrderInvoiceResponse>(Error.NotFound(
                "OrderInvoice.NotFound", $"Order {id} was not found."));
        }
        return Result.Success(OrderInvoiceResponse.From(order));
    }

    public async Task<Result<IReadOnlyList<PaymentLedgerRow>>> ListPaymentsAsync(CancellationToken ct)
    {
        // Reuses the existing Transactions DbSet — no new payments table needed for the admin
        // ledger view. Status is derived from presence of the gateway order id (verified) vs. not.
        var rows = await _db.Transactions.AsNoTracking()
            .OrderByDescending(t => t.Date)
            .Take(PaginationRequest.MaxPageSize)
            .Select(t => new PaymentLedgerRow(
                t.Id.ToString(),
                "Razorpay",
                t.Amount,
                t.Currency,
                t.GatewayOrderId != null ? "captured" : "pending",
                t.Date.ToString("O")))
            .ToListAsync(ct);
        return Result.Success((IReadOnlyList<PaymentLedgerRow>)rows);
    }

    public async Task<Result<IReadOnlyList<AdminAuditLogResponse>>> ListAuditLogsAsync(
        string? action, string? actorUserId, CancellationToken ct)
    {
        var q = _db.AdminAuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action))
        {
            var a = action.Trim().ToUpperInvariant();
            q = q.Where(l => l.Action == a);
        }
        if (!string.IsNullOrWhiteSpace(actorUserId))
        {
            var uid = actorUserId.Trim();
            q = q.Where(l => l.ActorUserId == uid);
        }
        var rows = await q
            .OrderByDescending(l => l.OccurredAtUtc)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<AdminAuditLogResponse>)rows.Select(AdminAuditLogResponse.From).ToList());
    }

    public async Task AppendAuditLogAsync(AdminAuditLog entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _db.AdminAuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
