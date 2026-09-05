namespace CareerPlatform.Api.Features.AdminLedger.Domain;

/// <summary>
/// The accepted values for <see cref="OrderInvoice.Status"/>. Declared as constants in one place so
/// the writer (payments flow), the reader (admin ledger service), and the schema CHECK semantics
/// can't drift apart — the audit flagged loose status strings duplicated across layers.
///
/// Deliberately a static class of <c>const string</c> rather than a C# <c>enum</c>: the column is
/// <c>character varying(16)</c> and these values are part of the JSON contract the admin UI filters
/// on, so the string IS the domain value.
/// </summary>
public static class OrderInvoiceStatus
{
    public const string Completed = "completed";
    public const string Pending = "pending";
    public const string Refunded = "refunded";
    public const string Failed = "failed";

    /// <summary>Every legal value; used to validate an inbound status filter.</summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Completed, Pending, Refunded, Failed,
        };

    /// <summary>True when <paramref name="value"/> is a recognised status.</summary>
    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value);
}
