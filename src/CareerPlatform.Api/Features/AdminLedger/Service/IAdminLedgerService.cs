using CareerPlatform.Api.Features.AdminLedger.Domain;
using CareerPlatform.Api.Features.AdminLedger.Dto;

namespace CareerPlatform.Api.Features.AdminLedger.Service;

/// <summary>
/// Read-only admin data-visibility surface. Bundles OrderInvoices, Payment ledger, and audit-logs
/// under one service because all three are simple filtered reads that don't share domain concerns
/// with each other — but do share the "admin observability" bounded context.
/// </summary>
public interface IAdminLedgerService
{
    Task<Result<IReadOnlyList<OrderInvoiceResponse>>> ListOrdersAsync(string? status, CancellationToken ct);

    Task<Result<OrderInvoiceResponse>> GetOrderAsync(int id, CancellationToken ct);

    Task<Result<IReadOnlyList<PaymentLedgerRow>>> ListPaymentsAsync(CancellationToken ct);

    Task<Result<IReadOnlyList<AdminAuditLogResponse>>> ListAuditLogsAsync(
        string? action, string? actorUserId, CancellationToken ct);

    /// <summary>Write hook used by the audit ActionFilter to append a log row.</summary>
    Task AppendAuditLogAsync(AdminAuditLog entry, CancellationToken ct);
}
