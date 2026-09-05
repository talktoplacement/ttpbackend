using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Interviews.Domain;

/// <summary>
/// A single interview grading-rubric axis (e.g. "Problem Deconstruction"). Admin-managed so the
/// evaluation criteria shown to mentors/AI are configurable, not hardcoded.
/// </summary>
public sealed class InterviewRubric : AuditableEntity<int>
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Relative weight of this axis in the overall score (0-100).</summary>
    public int Weight { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
