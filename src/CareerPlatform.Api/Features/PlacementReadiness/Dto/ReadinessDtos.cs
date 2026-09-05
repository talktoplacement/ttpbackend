namespace CareerPlatform.Api.Features.PlacementReadiness.Dto;

/// <summary>
/// One contributing dimension of the readiness score.
/// </summary>
/// <param name="Key">Stable machine key, safe for the client to switch on.</param>
/// <param name="Label">Display name.</param>
/// <param name="Weight">Share of the overall score, as a percentage of the total weight.</param>
/// <param name="Score">
/// 0-100 for this dimension, or <c>null</c> when the student has no data for it yet. Null is the whole
/// point of this contract: the page it replaces rendered "88%", "76%", "90%" and "82%" as literals, so
/// a student with no attempts still saw four confident bars.
/// </param>
/// <param name="SampleSize">How many underlying records the score is based on.</param>
/// <param name="Detail">Short explanation of what was measured.</param>
public sealed record ReadinessComponentResponse(
    string Key,
    string Label,
    int Weight,
    int? Score,
    int SampleSize,
    string Detail);

/// <summary>
/// A student's placement readiness, computed on read from live records.
/// </summary>
/// <param name="Score">
/// Weighted average across components that have data, or <c>null</c> when none do. Never a stand-in
/// value.
/// </param>
/// <param name="Band">Coarse label for <paramref name="Score"/>; empty when unscored.</param>
/// <param name="Coverage">
/// Percentage of the total component weight that actually had data. A score of 80 built from one
/// component out of five is not the same claim as 80 built from all five, and the UI must be able to
/// say so.
/// </param>
/// <param name="Components">Per-dimension breakdown, always in a stable order.</param>
/// <param name="ComputedAt">When this response was calculated.</param>
public sealed record ReadinessResponse(
    int? Score,
    string Band,
    int Coverage,
    IReadOnlyList<ReadinessComponentResponse> Components,
    DateTime ComputedAt);
