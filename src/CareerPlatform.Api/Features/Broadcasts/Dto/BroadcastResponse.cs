using CareerPlatform.Api.Features.Broadcasts.Domain;

namespace CareerPlatform.Api.Features.Broadcasts.Dto;

/// <summary>Outward-facing projection of a <see cref="Broadcast"/> reused by list and send.</summary>
public sealed record BroadcastResponse(
    int Id,
    string BroadcastType,
    string Heading,
    string TargetPlan,
    string? QuestionText,
    string? QuestionLink,
    string Message,
    int RecipientCount,
    DateTime SentAt)
{
    public static BroadcastResponse From(Broadcast b)
    {
        ArgumentNullException.ThrowIfNull(b);
        return new BroadcastResponse(
            b.Id, b.BroadcastType.ToString(), b.Heading, b.TargetPlan,
            b.QuestionText, b.QuestionLink, b.Message, b.RecipientCount, b.SentAtUtc);
    }
}

/// <summary>Send outcome carrying the persisted row and what was actually dispatched.</summary>
/// <param name="Broadcast">The persisted history row.</param>
/// <param name="RecipientCount">Students who received the in-app notification.</param>
/// <param name="EmailQueuedCount">
/// Addresses queued for e-mail delivery — non-zero only for <c>Promotion</c> broadcasts, and lower
/// than <paramref name="RecipientCount"/> when some targeted students have no address on file. The
/// UI must report this number rather than implying every recipient was e-mailed.
/// </param>
public sealed record SendBroadcastResult(
    BroadcastResponse Broadcast, int RecipientCount, int EmailQueuedCount);

/// <summary>Body for <c>POST /api/v1/admin/broadcasts</c>.</summary>
public sealed record SendBroadcastRequest(
    string BroadcastType,
    string Heading,
    string? TargetPlan,
    string? QuestionText,
    string? QuestionLink,
    string Message);

/// <summary>Response payload for the recipient-count endpoint.</summary>
public sealed record RecipientCountResult(int Count);

/// <summary>
/// One selectable broadcast audience. Derived from the live subscription-plan catalogue plus the
/// implicit "Free" and "All Plans" targets, so the admin UI never ships a hardcoded plan list that
/// can drift out of sync with the plans that actually exist.
/// </summary>
/// <param name="Value">The exact string to send back as <c>TargetPlan</c>.</param>
/// <param name="Label">Display text.</param>
/// <param name="RecipientCount">Students currently matching this target.</param>
public sealed record BroadcastAudienceTarget(string Value, string Label, int RecipientCount);
