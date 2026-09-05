using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Practice.Domain;

/// <summary>
/// A per-student bookmark on a practice question. The unique index on
/// <c>(UserId, PracticeQuestionId)</c> keeps the toggle-on endpoint idempotent — repeated calls
/// resolve to the same row.
/// </summary>
public sealed class PracticeBookmark : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string UserId { get; set; } = string.Empty;

    [Required] public int PracticeQuestionId { get; set; }

    /// <summary>Optional user-visible note captured at bookmark time.</summary>
    [MaxLength(1000)] public string? Notes { get; set; }

    [ForeignKey(nameof(PracticeQuestionId))]
    public PracticeQuestion? PracticeQuestion { get; set; }
}
