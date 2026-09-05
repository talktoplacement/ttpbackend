using CareerPlatform.Api.Features.AdminLedger.Domain;

namespace CareerPlatform.Api.Features.AdminLedger.Dto;

public sealed record OrderInvoiceResponse(
    int Id, string OrderId, string CustomerUserId, string? CustomerEmail,
    string ItemDescription, decimal Amount, string Currency, string Status,
    string PurchasedAt)
{
    public static OrderInvoiceResponse From(OrderInvoice o)
    {
        ArgumentNullException.ThrowIfNull(o);
        return new OrderInvoiceResponse(
            o.Id, o.OrderId, o.CustomerUserId, o.CustomerEmail,
            o.ItemDescription, o.Amount, o.Currency, o.Status,
            o.PurchasedAtUtc.ToString("O"));
    }
}

/// <summary>Admin view of a captured payment (Razorpay transaction row).</summary>
public sealed record PaymentLedgerRow(
    string Id, string Provider, decimal Amount, string Currency,
    string Status, string CreatedAt);

public sealed record AdminAuditLogResponse(
    long Id, string ActorUserId, string? ActorEmail, string Action,
    string? TargetKind, string? TargetId, string? Metadata,
    string? IpAddress, string OccurredAt)
{
    public static AdminAuditLogResponse From(AdminAuditLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return new AdminAuditLogResponse(
            log.Id, log.ActorUserId, log.ActorEmail, log.Action,
            log.TargetKind, log.TargetId, log.Metadata,
            log.IpAddress, log.OccurredAtUtc.ToString("O"));
    }
}
