using System.ComponentModel.DataAnnotations.Schema;
using CareerPlatform.Api.Features.Content.Domain;
using CareerPlatform.Api.Features.Users.Domain;

namespace CareerPlatform.Api.Features.Students.Domain;

/// <summary>
/// Per-student learning progress. Ported from the legacy entity with identical columns; only the
/// base type (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class ProgressLog : AggregateRoot<int>
{
    public string UserId { get; set; } = string.Empty;
    public int ContentId { get; set; }
    public int WatchDurationSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LogDate { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public UserProfile? User { get; set; }
    [ForeignKey("ContentId")]
    public CourseContent? Content { get; set; }
}
