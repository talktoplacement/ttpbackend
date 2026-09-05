using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Interviews.Domain;

/// <summary>
/// A student-owned mock-interview record. Populated at session creation (Scheduled) and updated
/// after the session runs to record score + rubric outcomes. The rubric is stored as JSON so the
/// schema stays flat while the frontend renders whatever criteria a session used.
/// </summary>
public sealed class MockInterviewSession : AuditableEntity<int>
{
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(32)] public string Type { get; set; } = "AI Mock";
    [Required, MaxLength(64)] public string Topic { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int? Score { get; set; }
    [Required, MaxLength(32)] public string Status { get; set; } = "scheduled";
    /// <summary>JSON-serialized rubric report. Empty until the session completes.</summary>
    public string RubricReportJson { get; set; } = "{}";
}
