using System.Text.Json;
using CareerPlatform.Api.Features.Interviews.Domain;

namespace CareerPlatform.Api.Features.Interviews.Dto;

public sealed record InterviewQuestionResponse(
    string Id, string Slug, string Prompt, string ExpectedAnswer,
    string Topic, string Difficulty, IReadOnlyList<string> CompanyTags, bool IsPublished)
{
    public static InterviewQuestionResponse From(InterviewQuestion q)
    {
        ArgumentNullException.ThrowIfNull(q);
        var tags = string.IsNullOrWhiteSpace(q.CompanyTags)
            ? Array.Empty<string>()
            : q.CompanyTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new InterviewQuestionResponse(
            q.Id.ToString(), q.Slug, q.Prompt, q.ExpectedAnswer,
            q.Topic, q.Difficulty, tags, q.IsPublished);
    }
}

public sealed record MockInterviewSessionResponse(
    string Id, string Type, string Topic, int DurationMinutes,
    int? Score, string Status, string CreatedAt, JsonElement? RubricReport)
{
    public static MockInterviewSessionResponse From(MockInterviewSession s)
    {
        ArgumentNullException.ThrowIfNull(s);
        JsonElement? rubric = null;
        if (!string.IsNullOrWhiteSpace(s.RubricReportJson) && s.RubricReportJson != "{}")
        {
            try
            {
                using var doc = JsonDocument.Parse(s.RubricReportJson);
                rubric = doc.RootElement.Clone();
            }
            catch (JsonException) { rubric = null; }
        }
        return new MockInterviewSessionResponse(
            s.Id.ToString(), s.Type, s.Topic, s.DurationMinutes,
            s.Score, s.Status, s.CreatedAtUtc.ToString("O"), rubric);
    }
}

/// <summary>
/// Admin view of a mock-interview session. Adds the owning user so admins can see whose session
/// it is; the student-facing <see cref="MockInterviewSessionResponse"/> deliberately omits it.
/// </summary>
public sealed record AdminMockInterviewSessionResponse(
    string Id, string UserId, string Type, string Topic, int DurationMinutes,
    int? Score, string Status, string CreatedAt)
{
    public static AdminMockInterviewSessionResponse From(MockInterviewSession s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new AdminMockInterviewSessionResponse(
            s.Id.ToString(), s.UserId, s.Type, s.Topic, s.DurationMinutes,
            s.Score, s.Status, s.CreatedAtUtc.ToString("O"));
    }
}

/// <summary>Body for POST /api/v1/admin/interview-questions.</summary>
public sealed record CreateInterviewQuestionRequest(
    string Slug, string Prompt, string? ExpectedAnswer, string Topic, string Difficulty,
    IReadOnlyList<string>? CompanyTags, bool IsPublished);

public sealed record UpdateInterviewQuestionRequest(
    string? Slug, string? Prompt, string? ExpectedAnswer, string? Topic, string? Difficulty,
    IReadOnlyList<string>? CompanyTags, bool? IsPublished);

public sealed record CreateInterviewSessionRequest(string Type, string Topic, int DurationMinutes);

public sealed record UpdateInterviewSessionRequest(string? Status, int? Score, string? RubricReportJson);
