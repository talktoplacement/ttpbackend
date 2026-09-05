using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Assessments.Domain;

/// <summary>
/// Admin-managed practice test / assessment. Question definitions are stored as JSON so schema
/// changes on the question shape are code-only. Rendering surfaces (upcoming / active /
/// completed) are computed by the frontend from <c>StartsAtUtc</c>/<c>EndsAtUtc</c>.
/// </summary>
public sealed class Assessment : AuditableEntity<int>
{
    [Required, MaxLength(160)] public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(4000)] public string Description { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string Category { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int PassingMarks { get; set; }
    public int QuestionsCount { get; set; }
    /// <summary>Optional scheduled window; when null the assessment is always available.</summary>
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    /// <summary>JSON-serialized question bank (server never introspects it — clients render).</summary>
    public string QuestionsJson { get; set; } = "[]";
    public bool IsPublished { get; set; } = true;
}
