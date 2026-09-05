using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.AdminLedger.Domain;

/// <summary>
/// Immutable audit trail row emitted whenever a privileged action lands. Derives from
/// <see cref="Entity{TId}"/> rather than <c>AuditableEntity</c>: this table IS the audit trail, so
/// created/updated-by metadata about it would be circular. The actor and
/// <see cref="OccurredAtUtc"/> ARE the payload.
/// </summary>
public sealed class AdminAuditLog : Entity<long>
{
    [Required, MaxLength(64)] public string ActorUserId { get; set; } = string.Empty;
    [MaxLength(320)] public string? ActorEmail { get; set; }
    [Required, MaxLength(64)] public string Action { get; set; } = string.Empty;
    [MaxLength(64)] public string? TargetKind { get; set; }
    [MaxLength(128)] public string? TargetId { get; set; }

    /// <summary>Arbitrary JSON payload with request/response snippets, ids, etc.</summary>
    public string? Metadata { get; set; }

    [MaxLength(64)] public string? IpAddress { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
