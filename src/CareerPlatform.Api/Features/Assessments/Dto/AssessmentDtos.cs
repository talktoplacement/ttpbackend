using System.Text.Json;
using CareerPlatform.Api.Features.Assessments.Domain;

namespace CareerPlatform.Api.Features.Assessments.Dto;

public sealed record AssessmentResponse(
    string Id, string Slug, string Title, string Description,
    int DurationMinutes, int TotalMarks, int PassingMarks, int QuestionsCount,
    string Category, string Status, string? StartsAt, string? EndsAt, bool IsPublished)
{
    public static AssessmentResponse From(Assessment a)
    {
        ArgumentNullException.ThrowIfNull(a);
        var now = DateTime.UtcNow;
        string status;
        if (a.StartsAtUtc is DateTime start && now < start) status = "upcoming";
        else if (a.EndsAtUtc is DateTime end && now > end) status = "completed";
        else status = "active";
        return new AssessmentResponse(
            a.Id.ToString(), a.Slug, a.Title, a.Description,
            a.DurationMinutes, a.TotalMarks, a.PassingMarks, a.QuestionsCount, a.Category,
            status, a.StartsAtUtc?.ToString("O"), a.EndsAtUtc?.ToString("O"), a.IsPublished);
    }
}

public sealed record AssessmentAttemptResponse(
    int Id, int AssessmentId, string AssessmentSlug, string AssessmentTitle,
    string StartedAt, string? SubmittedAt, int TotalMarks, int PassingMarks,
    int? Score, decimal? Percentile, int? TimeTakenMinutes, bool? Passed, JsonElement? Answers)
{
    public static AssessmentAttemptResponse From(AssessmentAttempt a, bool includeAnswers)
    {
        ArgumentNullException.ThrowIfNull(a);
        JsonElement? answers = null;
        if (includeAnswers && !string.IsNullOrWhiteSpace(a.AnswersJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(a.AnswersJson);
                answers = doc.RootElement.Clone();
            }
            catch (JsonException) { answers = null; }
        }
        return new AssessmentAttemptResponse(
            a.Id, a.AssessmentId,
            a.Assessment?.Slug ?? string.Empty, a.Assessment?.Title ?? string.Empty,
            a.StartedAtUtc.ToString("O"), a.SubmittedAtUtc?.ToString("O"),
            a.TotalMarks, a.PassingMarks,
            a.Score, a.Percentile, a.TimeTakenMinutes, a.Passed, answers);
    }
}

public sealed record CreateAssessmentRequest(
    string Slug, string Title, string? Description, int DurationMinutes,
    int TotalMarks, int PassingMarks, int QuestionsCount, string Category,
    string? StartsAtUtc, string? EndsAtUtc, bool IsPublished);

public sealed record UpdateAssessmentRequest(
    string? Slug, string? Title, string? Description, int? DurationMinutes,
    int? TotalMarks, int? PassingMarks, int? QuestionsCount, string? Category,
    string? StartsAtUtc, string? EndsAtUtc, bool? IsPublished);
