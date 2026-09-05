using CareerPlatform.Api.Features.Support.Domain;
using CareerPlatform.Api.Features.Support.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Support.Service;

/// <summary>Support tickets workflow. Ports the 6 legacy MediatR handlers verbatim.</summary>
internal sealed class SupportService : ISupportService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    public SupportService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<SupportTicketResponse>>> ListMineAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<SupportTicketResponse>>(Error.Unauthorized(
                "Support.Unauthorized", "An authenticated user is required."));
        }
        var rows = await _db.SupportTickets.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<SupportTicketResponse> items = rows.Select(t => SupportTicketResponse.From(t)).ToList();
        return Result.Success(items);
    }

    public async Task<Result<IReadOnlyList<SupportTicketResponse>>> ListAdminAsync(string? status, CancellationToken ct)
    {
        var q = _db.SupportTickets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            q = q.Where(t => t.Status == s);
        }
        var rows = await q
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<SupportTicketResponse> items = rows.Select(t => SupportTicketResponse.From(t)).ToList();
        return Result.Success(items);
    }

    public async Task<Result<SupportTicketResponse>> GetAsync(int id, bool allowAdmin, CancellationToken ct)
    {
        var ticket = await _db.SupportTickets.AsNoTracking()
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return Result.Failure<SupportTicketResponse>(Error.NotFound(
                "Support.NotFound", $"Ticket {id} was not found."));
        }
        if (!allowAdmin)
        {
            var caller = _currentUser.UserId;
            if (string.IsNullOrEmpty(caller) ||
                !string.Equals(ticket.UserId, caller, StringComparison.Ordinal))
            {
                return Result.Failure<SupportTicketResponse>(Error.NotFound(
                    "Support.NotFound", $"Ticket {id} was not found."));
            }
        }
        return Result.Success(SupportTicketResponse.From(ticket, includeMessages: true));
    }

    public async Task<Result<SupportTicketResponse>> CreateAsync(CreateTicketRequest r, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<SupportTicketResponse>(Error.Unauthorized(
                "Support.Unauthorized", "An authenticated user is required."));
        }
        var ticket = new SupportTicket
        {
            UserId = userId,
            Subject = r.Subject.Trim(),
            Category = r.Category.Trim(),
            Priority = string.IsNullOrWhiteSpace(r.Priority) ? "normal" : r.Priority.Trim().ToLowerInvariant(),
            Status = "open",
        };
        ticket.Messages.Add(new SupportTicketMessage
        {
            AuthorUserId = userId,
            AuthorRole = "Student",
            Body = r.Body,
        });
        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return Result.Success(SupportTicketResponse.From(ticket, includeMessages: true));
    }

    public async Task<Result<SupportTicketResponse>> PostMessageAsync(int ticketId, string body, bool allowAdmin, CancellationToken ct)
    {
        var caller = _currentUser.UserId;
        if (string.IsNullOrEmpty(caller))
        {
            return Result.Failure<SupportTicketResponse>(Error.Unauthorized(
                "Support.Unauthorized", "An authenticated user is required."));
        }
        var ticket = await _db.SupportTickets.Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null)
        {
            return Result.Failure<SupportTicketResponse>(Error.NotFound(
                "Support.NotFound", $"Ticket {ticketId} was not found."));
        }
        if (!allowAdmin && !string.Equals(ticket.UserId, caller, StringComparison.Ordinal))
        {
            return Result.Failure<SupportTicketResponse>(Error.NotFound(
                "Support.NotFound", $"Ticket {ticketId} was not found."));
        }

        var authorRole = allowAdmin ? "Admin" : "Student";
        ticket.Messages.Add(new SupportTicketMessage
        {
            TicketId = ticket.Id,
            AuthorUserId = caller,
            AuthorRole = authorRole,
            Body = body,
        });

        if (authorRole == "Student" && ticket.Status is "resolved" or "closed")
        {
            ticket.Status = "pending";
            ticket.ResolvedAtUtc = null;
            ticket.ClosedAtUtc = null;
        }
        else if (authorRole == "Admin" && ticket.Status == "open")
        {
            ticket.Status = "pending";
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success(SupportTicketResponse.From(ticket, includeMessages: true));
    }

    public async Task<Result<SupportTicketResponse>> UpdateStatusAsync(int id, UpdateTicketStatusRequest r, CancellationToken ct)
    {
        var ticket = await _db.SupportTickets.Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
        {
            return Result.Failure<SupportTicketResponse>(Error.NotFound(
                "Support.NotFound", $"Ticket {id} was not found."));
        }
        var newStatus = r.Status.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        if (newStatus == "resolved" && ticket.Status != "resolved")
        {
            ticket.ResolvedAtUtc = now;
            ticket.ClosedAtUtc = null;
        }
        else if (newStatus == "closed" && ticket.Status != "closed")
        {
            ticket.ClosedAtUtc = now;
            if (ticket.ResolvedAtUtc is null) ticket.ResolvedAtUtc = now;
        }
        else if (newStatus is "open" or "pending")
        {
            ticket.ResolvedAtUtc = null;
            ticket.ClosedAtUtc = null;
        }
        ticket.Status = newStatus;
        if (r.AssignedToUserId is not null)
        {
            ticket.AssignedToUserId = string.IsNullOrWhiteSpace(r.AssignedToUserId)
                ? null
                : r.AssignedToUserId.Trim();
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success(SupportTicketResponse.From(ticket, includeMessages: true));
    }
}
