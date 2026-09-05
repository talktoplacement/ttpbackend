using CareerPlatform.Api.BackgroundJobs;
using CareerPlatform.Api.BackgroundJobs.Jobs;
using CareerPlatform.Api.Features.Broadcasts.Domain;
using CareerPlatform.Api.Features.Broadcasts.Dto;
using CareerPlatform.Api.Features.Notifications.Domain;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Broadcasts.Service;

/// <summary>
/// Admin broadcasts workflow (list, audience count, send).
/// <para>
/// Send fans out one <c>Notification</c> row per targeted student in the same transaction as the
/// <c>Broadcast</c> audit record, so history and inbox can never disagree. For
/// <see cref="BroadcastType.Promotion"/> the e-mail leg is additionally queued as a background
/// <see cref="EmailJob"/> — deliberately *after* the commit, because a provider outage must not
/// roll back a broadcast that recipients can already see in-app, and because sending N messages
/// inline would hold the request open for the whole batch.
/// </para>
/// </summary>
internal sealed class BroadcastService : IBroadcastService
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobScheduler _jobs;
    private readonly ICurrentUser _currentUser;

    public BroadcastService(AppDbContext db, IBackgroundJobScheduler jobs, ICurrentUser currentUser)
    {
        _db = db;
        _jobs = jobs;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<BroadcastResponse>>> ListAsync(
        string? type, CancellationToken ct)
    {
        var query = _db.Broadcasts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(type)
            && Enum.TryParse<BroadcastType>(type, ignoreCase: true, out var typed))
        {
            query = query.Where(b => b.BroadcastType == typed);
        }

        var rows = await query
            .OrderByDescending(b => b.SentAtUtc)
            .ThenByDescending(b => b.Id)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);

        IReadOnlyList<BroadcastResponse> items = rows.Select(BroadcastResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<RecipientCountResult>> GetRecipientCountAsync(
        string? targetPlan, CancellationToken ct)
    {
        var count = await AudienceResolver.CountAsync(_db, targetPlan, ct);
        return Result.Success(new RecipientCountResult(count));
    }

    public async Task<Result<IReadOnlyList<BroadcastResponse>>> ListTodayForCurrentStudentAsync(
        CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<BroadcastResponse>>(Error.Unauthorized(
                "Broadcast.Unauthorized",
                "An authenticated student is required to read today's messages."));
        }

        // The student's plan is read server-side; accepting it from the client would let anyone
        // enumerate broadcasts aimed at plans they have not paid for.
        var planName = await _db.UserProfiles.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PlanName)
            .FirstOrDefaultAsync(ct);

        if (planName is null)
        {
            return Result.Failure<IReadOnlyList<BroadcastResponse>>(Error.NotFound(
                "Broadcast.ProfileNotFound", "No profile exists for the signed-in user."));
        }

        var sinceUtc = DateTime.UtcNow.Date;

        var rows = await _db.Broadcasts.AsNoTracking()
            .Where(b => b.BroadcastType == BroadcastType.Notification
                        && b.SentAtUtc >= sinceUtc
                        && (b.TargetPlan == AudienceResolver.AllPlans || b.TargetPlan == planName))
            .OrderByDescending(b => b.SentAtUtc)
            .ThenByDescending(b => b.Id)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);

        IReadOnlyList<BroadcastResponse> items = rows.Select(BroadcastResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<IReadOnlyList<BroadcastAudienceTarget>>> ListAudienceTargetsAsync(
        CancellationToken ct)
    {
        // Group the students themselves rather than the plan catalogue: this returns every plan
        // label that real users actually carry (including historical ones whose plan row was since
        // deactivated), each with its true reachable count.
        var byPlan = await _db.UserProfiles.AsNoTracking()
            .Where(u => u.Role == AudienceResolver.StudentRole)
            .GroupBy(u => u.PlanName)
            .Select(g => new { Plan = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Plans that exist but currently have no subscriber still belong in the dropdown, otherwise
        // a newly-launched plan is unreachable until someone buys it.
        var catalogueNames = await _db.SubscriptionPlans.AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.Name)
            .ToListAsync(ct);

        var counts = byPlan
            .Where(x => !string.IsNullOrWhiteSpace(x.Plan))
            .ToDictionary(x => x.Plan, x => x.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var name in catalogueNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            counts.TryAdd(name, 0);
        }

        var targets = new List<BroadcastAudienceTarget>
        {
            new(AudienceResolver.AllPlans, AudienceResolver.AllPlans, byPlan.Sum(x => x.Count)),
        };
        targets.AddRange(counts
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new BroadcastAudienceTarget(kv.Key, kv.Key, kv.Value)));

        IReadOnlyList<BroadcastAudienceTarget> result = targets;
        return Result.Success(result);
    }

    public async Task<Result<SendBroadcastResult>> SendAsync(
        SendBroadcastRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.TryParse<BroadcastType>(request.BroadcastType, ignoreCase: true, out var type))
        {
            return Result.Failure<SendBroadcastResult>(Error.Validation(
                "Broadcast.UnknownType",
                $"'{request.BroadcastType}' is not a supported broadcast type."));
        }

        var target = AudienceResolver.NormalizeTarget(request.TargetPlan);
        var recipients = await AudienceResolver.Resolve(_db, target).ToListAsync(ct);

        if (recipients.Count == 0)
        {
            // Recording a broadcast nobody receives would put a misleading row in the history and
            // report success for a no-op. Let the admin correct the target instead.
            return Result.Failure<SendBroadcastResult>(Error.Validation(
                "Broadcast.EmptyAudience",
                $"No students currently match the target '{target}', so there is nobody to send to."));
        }

        var broadcast = new Broadcast
        {
            BroadcastType = type,
            Heading = request.Heading.Trim(),
            TargetPlan = target,
            QuestionText = Trimmed(request.QuestionText),
            QuestionLink = Trimmed(request.QuestionLink),
            Message = request.Message.Trim(),
            RecipientCount = recipients.Count,
            SentAtUtc = DateTime.UtcNow,
        };
        _db.Broadcasts.Add(broadcast);

        var notificationType = type == BroadcastType.Promotion ? "promotion" : "system";
        foreach (var recipient in recipients)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = recipient.UserId,
                Type = notificationType,
                Title = broadcast.Heading,
                Body = broadcast.Message,
                ActionUrl = broadcast.QuestionLink,
                CreatedAt = broadcast.SentAtUtc,
                IsRead = false,
                IsDismissed = false,
            });
        }

        await _db.SaveChangesAsync(ct);

        var emailRecipients = type == BroadcastType.Promotion
            ? recipients
                .Select(r => r.Email)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .ToList()
            : [];

        if (emailRecipients.Count > 0)
        {
            await _jobs.EnqueueAsync(
                new EmailJob(emailRecipients, broadcast.Heading, BroadcastEmailBody.Render(broadcast)),
                ct);
        }

        return Result.Success(new SendBroadcastResult(
            BroadcastResponse.From(broadcast),
            recipients.Count,
            emailRecipients.Count));
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
