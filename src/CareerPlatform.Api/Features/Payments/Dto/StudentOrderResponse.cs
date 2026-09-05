using CareerPlatform.Api.Features.AdminLedger.Domain;

namespace CareerPlatform.Api.Features.Payments.Dto;

/// <summary>
/// A single purchase in the caller's own order history, projected from the denormalised
/// <see cref="OrderInvoice"/> ledger row. Customer identity is implied (it is the authenticated
/// caller), so — unlike the admin <c>OrderInvoiceResponse</c> — no customer id/email is exposed.
/// </summary>
public sealed record StudentOrderResponse(
    int Id,
    string OrderId,
    string ItemDescription,
    decimal Amount,
    string Currency,
    string Status,
    string PurchasedAt)
{
    public static StudentOrderResponse From(OrderInvoice o)
    {
        ArgumentNullException.ThrowIfNull(o);
        return new StudentOrderResponse(
            o.Id, o.OrderId, o.ItemDescription, o.Amount, o.Currency, o.Status,
            o.PurchasedAtUtc.ToString("O"));
    }
}
