using CareerPlatform.Api.Features.Posts.Domain;

namespace CareerPlatform.Api.Features.Posts.Dto;

// ── Response DTOs ────────────────────────────────────────────────────────────

/// <summary>Full post projection (author self-view + public read + admin review).</summary>
public sealed record PostResponse(
    int Id,
    string AuthorUserId,
    string? AuthorName,
    string Title,
    string Slug,
    string ContentMarkdown,
    string? Excerpt,
    string? CoverImageUrl,
    IReadOnlyList<string> Tags,
    string Status,
    int WordCount,
    int CharCount,
    string? SubmittedAt,
    string? PublishedAt,
    string? ReviewedAt,
    string? ReviewNote,
    string CreatedAt,
    string? UpdatedAt)
{
    public static PostResponse From(Post p, string? authorName = null) =>
        new(
            p.Id,
            p.AuthorUserId,
            authorName,
            p.Title,
            p.Slug,
            p.ContentMarkdown,
            string.IsNullOrWhiteSpace(p.Excerpt) ? null : p.Excerpt,
            string.IsNullOrWhiteSpace(p.CoverImageUrl) ? null : p.CoverImageUrl,
            SplitTags(p.Tags),
            p.Status,
            p.WordCount,
            p.CharCount,
            p.SubmittedAtUtc?.ToString("O"),
            p.PublishedAtUtc?.ToString("O"),
            p.ReviewedAtUtc?.ToString("O"),
            string.IsNullOrWhiteSpace(p.ReviewNote) ? null : p.ReviewNote,
            p.CreatedAtUtc.ToString("O"),
            p.UpdatedAtUtc?.ToString("O"));

    internal static IReadOnlyList<string> SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? Array.Empty<string>()
            : tags.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>Lightweight projection for lists (no body).</summary>
public sealed record PostSummaryResponse(
    int Id,
    string AuthorUserId,
    string? AuthorName,
    string Title,
    string Slug,
    string? Excerpt,
    string? CoverImageUrl,
    IReadOnlyList<string> Tags,
    string Status,
    int WordCount,
    string? SubmittedAt,
    string? PublishedAt,
    string CreatedAt,
    string? UpdatedAt)
{
    public static PostSummaryResponse From(Post p, string? authorName = null) =>
        new(
            p.Id,
            p.AuthorUserId,
            authorName,
            p.Title,
            p.Slug,
            string.IsNullOrWhiteSpace(p.Excerpt) ? null : p.Excerpt,
            string.IsNullOrWhiteSpace(p.CoverImageUrl) ? null : p.CoverImageUrl,
            PostResponse.SplitTags(p.Tags),
            p.Status,
            p.WordCount,
            p.SubmittedAtUtc?.ToString("O"),
            p.PublishedAtUtc?.ToString("O"),
            p.CreatedAtUtc.ToString("O"),
            p.UpdatedAtUtc?.ToString("O"));
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Body for creating or updating a draft (the "Save for later" payload). The author identity,
/// slug, status, and counts are all derived server-side — never accepted from the client.
/// </summary>
public sealed record PostEditorRequest(
    string Title,
    string ContentMarkdown,
    string? Excerpt,
    string? CoverImageUrl,
    string? Tags);

/// <summary>Admin review decision. <c>Decision</c> is "approve" or "reject".</summary>
public sealed record ReviewPostRequest(string Decision, string? Note);
