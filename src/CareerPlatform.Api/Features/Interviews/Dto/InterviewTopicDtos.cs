namespace CareerPlatform.Api.Features.Interviews.Dto;

/// <summary>
/// A topic in the interview question bank, derived from the questions themselves.
/// </summary>
/// <param name="Topic">The topic value as stored on the questions; also the filter value to send back.</param>
/// <param name="QuestionCount">Published questions in this topic. A real count, not a "120+" claim.</param>
/// <param name="DifficultyCounts">Published questions per difficulty, ordered easiest first where recognised.</param>
/// <param name="CompanyTags">Distinct company tags actually present on this topic's questions.</param>
/// <param name="MySessionCount">
/// Mock-interview sessions the caller has created on this topic. Replaces the hardcoded "Enrolled"
/// badge, which asserted a per-student fact from a literal and so was identical for every account.
/// </param>
/// <param name="MyCompletedSessionCount">How many of those the caller finished.</param>
/// <param name="MyBestScore">Best score across the caller's scored sessions, or null if none.</param>
public sealed record InterviewTopicResponse(
    string Topic,
    int QuestionCount,
    IReadOnlyList<DifficultyCount> DifficultyCounts,
    IReadOnlyList<string> CompanyTags,
    int MySessionCount,
    int MyCompletedSessionCount,
    int? MyBestScore);

public sealed record DifficultyCount(string Difficulty, int Count);
