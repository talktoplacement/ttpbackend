using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Interviews.Domain;

/// <summary>
/// Curated interview-question bank shown on the public interview-prep pages. Admin-managed.
/// </summary>
public sealed class InterviewQuestion : AuditableEntity<int>
{
    [Required, MaxLength(160)] public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string Prompt { get; set; } = string.Empty;
    [MaxLength(8000)] public string ExpectedAnswer { get; set; } = string.Empty;
    /// <summary>DSA / System Design / Frontend / Behavioural — free-text but conventionally the frontend `InterviewTopic`.</summary>
    [Required, MaxLength(64)] public string Topic { get; set; } = string.Empty;
    [Required, MaxLength(16)] public string Difficulty { get; set; } = "Easy";
    /// <summary>Comma-separated company tags.</summary>
    [MaxLength(500)] public string CompanyTags { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
}
