using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerPlatform.Api.Features.Content.Domain;

/// <summary>
/// Child of <see cref="Language"/>. Ported from the legacy entity with identical columns; only the
/// base type (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class Section : AggregateRoot<int>
{
    [Required]
    public int LanguageId { get; set; }
    [Required]
    public string Title { get; set; } = string.Empty; // e.g. "Basics"
    public int OrderIndex { get; set; } = 0; // Used for sorting

    [ForeignKey("LanguageId")]
    public Language? Language { get; set; }

    // Navigation property
    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
