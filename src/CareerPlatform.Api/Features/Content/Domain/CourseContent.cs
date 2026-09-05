namespace CareerPlatform.Api.Features.Content.Domain;

/// <summary>
/// Lesson/content body. Ported from the legacy entity with identical columns; only the base type
/// (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class CourseContent : AggregateRoot<int>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // e.g. "AI-ML", "System Design"
    public string FileUrl { get; set; } = string.Empty;
    public bool IsPaid { get; set; } = false;
    public bool IsPublished { get; set; } = true;
    public decimal Price { get; set; } = 0;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
