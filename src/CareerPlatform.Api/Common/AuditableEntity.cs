namespace CareerPlatform.Api.Common;

/// <summary>
/// Base type for aggregate roots carrying automatically-populated audit fields. The
/// <c>AuditableEntityInterceptor</c> sets created/updated fields during SaveChanges via the
/// <see cref="IAuditable"/> contract (Req 10). On insert the updated fields remain null (Req 10.4).
/// </summary>
/// <typeparam name="TId">The identifier type; must be non-null.</typeparam>
public abstract class AuditableEntity<TId> : AggregateRoot<TId>, IAuditable
    where TId : notnull
{
    /// <inheritdoc />
    public DateTime CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public string CreatedBy { get; set; } = default!;

    /// <inheritdoc />
    public DateTime? UpdatedAtUtc { get; set; }

    /// <inheritdoc />
    public string? UpdatedBy { get; set; }
}
