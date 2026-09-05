using CareerPlatform.Api.Features.Notifications.Domain;
using CareerPlatform.Api.Features.Notifications.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Notifications.Service;

/// <summary>Notifications workflow. Ports the 5 legacy MediatR handlers verbatim.</summary>
internal sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    public NotificationService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<NotificationResponse>>> ListMineAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<NotificationResponse>>(Error.Unauthorized(
                "Notification.Unauthorized", "An authenticated user is required to read notifications."));
        }
        var rows = await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDismissed)
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<NotificationResponse> mapped = rows.Select(NotificationResponse.From).ToList();
        return Result.Success(mapped);
    }

    public async Task<Result> MarkReadAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(Error.Unauthorized(
                "Notification.Unauthorized", "An authenticated user is required to update notifications."));
        }
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (n is null)
        {
            return Result.Failure(Error.NotFound("Notification.NotFound", $"Notification {id} was not found."));
        }
        if (!n.IsRead)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<Result> MarkAllReadAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(Error.Unauthorized(
                "Notification.Unauthorized", "An authenticated user is required to update notifications."));
        }
        var rows = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsDismissed && !n.IsRead).ToListAsync(ct);
        foreach (var row in rows) row.IsRead = true;
        if (rows.Count > 0) await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ClearAllAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(Error.Unauthorized(
                "Notification.Unauthorized", "An authenticated user is required to clear notifications."));
        }
        var rows = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsDismissed).ToListAsync(ct);
        foreach (var row in rows) row.IsDismissed = true;
        if (rows.Count > 0) await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<PublishNotificationResult>> PublishAsync(PublishNotificationRequest r, CancellationToken ct)
    {
        var target = NormalizeTarget(r.TargetRole);
        var query = _db.UserProfiles.AsQueryable();
        if (!string.IsNullOrEmpty(target))
        {
            query = query.Where(u => u.Role == target);
        }
        var userIds = await query.Select(u => u.Id).ToListAsync(ct);
        if (userIds.Count == 0)
        {
            return Result.Success(new PublishNotificationResult(0));
        }
        var now = DateTime.UtcNow;
        var type = r.Type ?? "system";
        foreach (var uid in userIds)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = uid, Type = type, Title = r.Title, Body = r.Body,
                ActionUrl = r.ActionUrl, CreatedAt = now, IsRead = false, IsDismissed = false,
            });
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success(new PublishNotificationResult(userIds.Count));
    }

    private static string? NormalizeTarget(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return null;
        var trimmed = role.Trim();
        if (string.Equals(trimmed, "all", StringComparison.OrdinalIgnoreCase)) return null;
        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..].ToLowerInvariant();
    }
}
