namespace CareerPlatform.Api.Common;

/// <summary>
/// Marker contract exposing the four audit fields so the <c>AuditableEntityInterceptor</c> can
/// populate them during SaveChanges without knowing the entity's identifier type (Req 10).
/// </summary>
public interface IAuditable
{
    /// <summary>UTC timestamp captured when the entity was inserted.</summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>Identifier of the user (or "system") that created the entity.</summary>
    string CreatedBy { get; set; }

    /// <summary>UTC timestamp of the last modification; null on insert (Req 10.4).</summary>
    DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Identifier of the user that last modified the entity; null on insert (Req 10.4).</summary>
    string? UpdatedBy { get; set; }
}
