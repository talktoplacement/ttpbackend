using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Support.Domain;

/// <summary>
/// A student-raised support ticket. The append-only conversation lives in
/// <see cref="SupportTicketMessage"/> rows keyed on <see cref="Id"/>. Status flows:
/// <c>open → pending → resolved → closed</c>; only admins can set <c>resolved</c> or
/// <c>closed</c>. Only the owner (or an admin) can read a ticket.
/// </summary>
public sealed class SupportTicket : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(200)] public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Free-text category label — conventionally one of <c>Billing</c>, <c>Technical</c>,
    /// <c>Mentorship</c>, <c>Curriculum</c>, or <c>Other</c>. Enforced at the API layer.
    /// </summary>
    [Required, MaxLength(64)] public string Category { get; set; } = "Other";

    /// <summary>Workflow status. <c>open | pending | resolved | closed</c>.</summary>
    [Required, MaxLength(32)] public string Status { get; set; } = "open";

    /// <summary>Priority label. <c>low | normal | high | urgent</c>.</summary>
    [Required, MaxLength(16)] public string Priority { get; set; } = "normal";

    /// <summary>Admin who owns triage; null when unassigned.</summary>
    [MaxLength(64)] public string? AssignedToUserId { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    /// <summary>Message thread — populated via the messages endpoint.</summary>
    public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
}
