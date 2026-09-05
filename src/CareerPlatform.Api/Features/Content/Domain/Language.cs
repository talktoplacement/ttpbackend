using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Features.Content.Domain;

/// <summary>
/// Tutorial curriculum root. Ported from the legacy entity with identical columns; only the base
/// type (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class Language : AggregateRoot<int>
{
    [Required]
    public string Title { get; set; } = string.Empty; // e.g. "Java"
    [Required]
    public string Slug { get; set; } = string.Empty;  // e.g. "java"
    public string Description { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
    public decimal Price { get; set; } = 0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<Section> Sections { get; set; } = new List<Section>();
}
