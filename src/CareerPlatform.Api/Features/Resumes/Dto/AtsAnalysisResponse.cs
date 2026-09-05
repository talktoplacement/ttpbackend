namespace CareerPlatform.Api.Features.Resumes.Dto;

/// <summary>A single scored dimension of the ATS analysis (0-100) with a short explanation.</summary>
public sealed record AtsSubScore(string Key, string Label, int Score, string Detail);

/// <summary>
/// Result of a deterministic ATS scan of a resume's extracted text. Every number is computed
/// from the actual text — there are no fabricated or random values.
/// </summary>
public sealed record AtsAnalysisResponse(
    int OverallScore,
    string Grade,
    int WordCount,
    IReadOnlyList<AtsSubScore> SubScores,
    IReadOnlyList<string> MatchedKeywords,
    IReadOnlyList<string> MissingKeywords,
    IReadOnlyList<string> Suggestions);
