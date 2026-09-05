using CareerPlatform.Api.Features.Practice.Domain;

namespace CareerPlatform.Api.Features.Practice.Dto;

public sealed record PracticeQuestionResponse(
    string Id, string Slug, string Title, string Description,
    string Difficulty, string Category, int AcceptanceRate,
    IReadOnlyList<string> CompanyTags, bool IsPublished)
{
    public static PracticeQuestionResponse From(PracticeQuestion q)
    {
        ArgumentNullException.ThrowIfNull(q);
        var tags = string.IsNullOrWhiteSpace(q.CompanyTags)
            ? Array.Empty<string>()
            : q.CompanyTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new PracticeQuestionResponse(
            q.Id.ToString(), q.Slug, q.Title, q.Description, q.Difficulty, q.Category,
            q.AcceptanceRate, tags, q.IsPublished);
    }
}

public sealed record PracticeBookmarkResponse(
    int Id, int PracticeQuestionId, string QuestionSlug, string QuestionTitle,
    string QuestionDifficulty, string QuestionCategory, string? Notes, string CreatedAt)
{
    public static PracticeBookmarkResponse From(PracticeBookmark b)
    {
        ArgumentNullException.ThrowIfNull(b);
        var q = b.PracticeQuestion;
        return new PracticeBookmarkResponse(
            b.Id, b.PracticeQuestionId,
            q?.Slug ?? string.Empty, q?.Title ?? string.Empty,
            q?.Difficulty ?? string.Empty, q?.Category ?? string.Empty,
            b.Notes, b.CreatedAtUtc.ToString("O"));
    }
}

public sealed record CreatePracticeQuestionRequest(
    string Slug, string Title, string? Description, string Difficulty, string Category,
    int AcceptanceRate, IReadOnlyList<string>? CompanyTags, bool IsPublished);

public sealed record UpdatePracticeQuestionRequest(
    string? Slug, string? Title, string? Description, string? Difficulty, string? Category,
    int? AcceptanceRate, IReadOnlyList<string>? CompanyTags, bool? IsPublished);

public sealed record ToggleBookmarkRequest(string? Notes);
