using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.MentorAssignments.Domain;

/// <summary>
/// A dedicated mentor↔student pairing within a cohort. Business rule: a student may have at most
/// ONE active assignment at a time (<c>EndedAtUtc IS NULL</c>) — enforced both by the partial
/// unique index in <c>schema.sql</c> and by an explicit check in the service so the caller gets a
/// readable error instead of a DB constraint violation.
///
/// Ending an assignment is a soft close (set <see cref="EndedAtUtc"/>) rather than a delete, so
/// the pairing history survives for reporting.
/// </summary>
public sealed class MentorAssignment : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string StudentUserId { get; set; } = string.Empty;

    public int MentorId { get; set; }

    [MaxLength(128)] public string? CohortName { get; set; }

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Null while the assignment is active. Set on unassign.</summary>
    public DateTime? EndedAtUtc { get; set; }

    [MaxLength(1000)] public string? Notes { get; set; }

    /// <summary>Convenience projection used by the service's business rules.</summary>
    public bool IsActive => EndedAtUtc is null;
}
