using CareerPlatform.Api.Features.Support.Domain;

namespace CareerPlatform.Api.Features.Support.Dto;

public sealed record SupportTicketMessageDto(
    int Id, string AuthorUserId, string AuthorRole, string Body, string CreatedAt);

public sealed record SupportTicketResponse(
    int Id, string UserId, string Subject, string Category, string Status, string Priority,
    string? AssignedToUserId, string CreatedAt, string? UpdatedAt,
    string? ResolvedAt, string? ClosedAt,
    IReadOnlyList<SupportTicketMessageDto> Messages)
{
    public static SupportTicketResponse From(SupportTicket t, bool includeMessages = false)
    {
        ArgumentNullException.ThrowIfNull(t);
        var messages = includeMessages
            ? t.Messages
                .OrderBy(m => m.CreatedAtUtc)
                .Select(m => new SupportTicketMessageDto(
                    m.Id, m.AuthorUserId, m.AuthorRole, m.Body, m.CreatedAtUtc.ToString("O")))
                .ToList()
            : new List<SupportTicketMessageDto>();
        return new SupportTicketResponse(
            t.Id, t.UserId, t.Subject, t.Category, t.Status, t.Priority,
            t.AssignedToUserId, t.CreatedAtUtc.ToString("O"), t.UpdatedAtUtc?.ToString("O"),
            t.ResolvedAtUtc?.ToString("O"), t.ClosedAtUtc?.ToString("O"),
            messages);
    }
}

public sealed record CreateTicketRequest(string Subject, string Category, string? Priority, string Body);
public sealed record PostTicketMessageRequest(string Body);
public sealed record UpdateTicketStatusRequest(string Status, string? AssignedToUserId);
