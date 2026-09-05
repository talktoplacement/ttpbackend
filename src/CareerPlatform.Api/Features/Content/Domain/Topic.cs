using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerPlatform.Api.Features.Content.Domain;

/// <summary>
/// Child of <see cref="Section"/>. Carries the markdown body plus optional interview-question
/// metadata (company tags, frequency, difficulty, read time) so the same table can back both
/// the tutorial CMS and the interview-question pages without a second entity graph.
/// </summary>
public class Topic : AggregateRoot<int>
{
    [Required]
    public int SectionId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Body content authored as Markdown (supports fenced code blocks and KaTeX math).</summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>Explicit ordering within the parent section (ascending).</summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// Comma-separated company names for the "Asked in" pill row (e.g. "Amazon, Google, Microsoft").
    /// Stored flat so the schema stays simple; the frontend splits on comma.
    /// </summary>
    [MaxLength(500)]
    public string? CompanyTags { get; set; }

    /// <summary>Free-text frequency label — conventionally "Low" / "Medium" / "High" / "Very High".</summary>
    [MaxLength(32)]
    public string? Frequency { get; set; }

    /// <summary>Free-text difficulty label — conventionally "Easy" / "Medium" / "Hard".</summary>
    [MaxLength(32)]
    public string? Difficulty { get; set; }

    /// <summary>Optional read-time estimate in minutes; 0/null hides the pill.</summary>
    public int? ReadTimeMinutes { get; set; }

    /// <summary>
    /// When true, this topic is premium: it stays visible in listings/curriculum but its body is
    /// withheld from users without an active paid subscription (the public API returns it locked).
    /// Free topics (the default) are readable by everyone.
    /// </summary>
    public bool IsPaid { get; set; } = false;

    /// <summary>Timestamp of the most recent content update (used for the "Last Updated" line).</summary>
    public DateTime? LastUpdatedUtc { get; set; }

    [ForeignKey("SectionId")]
    public Section? Section { get; set; }
}
