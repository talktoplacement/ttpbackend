using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CareerPlatform.Api.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges interceptor that populates the <see cref="IAuditable"/> audit fields automatically
/// (Req 10). A single UTC timestamp is captured at the start of the operation and applied to every
/// affected entity (Req 10.1, 10.6). Inserted entities get created-at/created-by set with
/// updated-at/updated-by left null (Req 10.1, 10.4); modified entities get updated-at/updated-by set
/// while their created-at/created-by remain unchanged (Req 10.2, 10.5). The created-by/updated-by
/// value is the <see cref="ICurrentUser.UserId"/>, or the reserved value <c>"system"</c> when no
/// authenticated principal is available (Req 10.3).
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    /// <summary>The reserved created-by/updated-by value used when there is no current user (Req 10.3).</summary>
    public const string SystemUser = "system";

    private readonly ICurrentUser _currentUser;

    public AuditableEntityInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // One UTC timestamp for the whole operation, shared by every affected entity (Req 10.1, 10.6).
        var now = DateTime.UtcNow;
        var who = _currentUser.UserId ?? SystemUser; // "system" when unauthenticated (Req 10.3).

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedBy = who;
                // Updated-at/updated-by remain null on insert (Req 10.4).
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedBy = who;

                // Preserve the original created-at/created-by values on update (Req 10.5).
                entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
                entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
            }
        }
    }
}
