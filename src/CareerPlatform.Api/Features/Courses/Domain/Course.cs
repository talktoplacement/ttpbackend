using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Features.Courses.Domain;

/// <summary>
/// A purchasable learning product, distinct from free tutorial content. Ported from the legacy
/// entity with identical columns; only the base type (<see cref="AggregateRoot{TId}"/>) and
/// namespace change (Req 9, 24.5).
/// </summary>
public class Course : AggregateRoot<int>
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Catalog price in INR (whole rupees). Server-controlled.</summary>
    public decimal Price { get; set; } = 0;

    public bool IsPublished { get; set; } = true;

    public string? MediaUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
