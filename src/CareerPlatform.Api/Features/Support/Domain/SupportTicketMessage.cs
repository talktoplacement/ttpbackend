using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Support.Domain;

/// <summary>
/// One entry in a <see cref="SupportTicket"/> thread. Append-only from the client's point of
/// view — there is no edit endpoint. <see cref="AuthorRole"/> is stamped by the handler based
/// on the JWT so the client cannot spoof "Admin" replies.
/// </summary>
public sealed class SupportTicketMessage : AuditableEntity<int>
{
    [Required] public int TicketId { get; set; }

    [Required, MaxLength(64)] public string AuthorUserId { get; set; } = string.Empty;

    [Required, MaxLength(16)] public string AuthorRole { get; set; } = "Student";

    [Required] public string Body { get; set; } = string.Empty;

    [ForeignKey(nameof(TicketId))]
    public SupportTicket? Ticket { get; set; }
}
