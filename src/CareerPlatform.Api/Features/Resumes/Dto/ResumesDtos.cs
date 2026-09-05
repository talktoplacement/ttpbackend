using CareerPlatform.Api.Features.Resumes.Domain;

namespace CareerPlatform.Api.Features.Resumes.Dto;

// ── Response DTOs ────────────────────────────────────────────────────────────

/// <summary>Matches the frontend `ResumeItem` shape.</summary>
public sealed record ResumeSubmissionResponse(
    string Id, string Title, string Template, int? AtsScore, string UpdatedAt)
{
    public static ResumeSubmissionResponse From(ResumeSubmission s)
    {
        ArgumentNullException.ThrowIfNull(s);
        var updated = s.UpdatedAtUtc ?? s.CreatedAtUtc;
        return new ResumeSubmissionResponse(
            s.Id.ToString(), s.Title, s.TemplateCode, s.AtsScore, updated.ToString("O"));
    }
}

public sealed record ResumeTemplateResponse(
    string Id, string Code, string Name, string Description, string? PreviewUrl, bool IsPublished)
{
    public static ResumeTemplateResponse From(ResumeTemplate t)
    {
        ArgumentNullException.ThrowIfNull(t);
        return new ResumeTemplateResponse(
            t.Id.ToString(), t.Code, t.Name, t.Description,
            string.IsNullOrWhiteSpace(t.PreviewUrl) ? null : t.PreviewUrl, t.IsPublished);
    }
}

/// <summary>
/// Outward-facing shape for a stored PDF resume upload. Shared by student, admin and mentor
/// endpoints. <c>StorageKey</c> is never exposed — download endpoints stream the file directly.
/// </summary>
public sealed record StudentResumeUploadResponse(
    int Id,
    string StudentUserId,
    string? StudentFullName,
    string? StudentEmail,
    string OriginalFileName,
    long SizeBytes,
    string UploadedAt,
    string ExpiresAt,
    string? AssignedMentorUserId,
    string? AssignedMentorFullName,
    string? AssignedAt)
{
    public static StudentResumeUploadResponse From(
        StudentResumeUpload upload,
        string? studentFullName = null,
        string? studentEmail = null,
        string? mentorFullName = null)
    {
        ArgumentNullException.ThrowIfNull(upload);
        return new StudentResumeUploadResponse(
            upload.Id,
            upload.StudentUserId,
            studentFullName,
            studentEmail,
            upload.OriginalFileName,
            upload.SizeBytes,
            upload.UploadedAtUtc.ToString("O"),
            upload.ExpiresAtUtc.ToString("O"),
            upload.AssignedMentorUserId,
            mentorFullName,
            upload.AssignedAtUtc?.ToString("O"));
    }
}

/// <summary>Streamable payload — never exposed on the wire; used only inside the download flow.</summary>
public sealed record ResumeDownloadPayload(byte[] Content, string FileName, string ContentType);

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record CreateMyResumeRequest(string Title, string TemplateCode, string? StorageKey);

public sealed record UpdateMyResumeRequest(string? Title, string? TemplateCode, string? StorageKey);

public sealed record CreateResumeTemplateRequest(
    string Code, string Name, string? Description, string? PreviewUrl, bool IsPublished);

public sealed record UpdateResumeTemplateRequest(
    string? Code, string? Name, string? Description, string? PreviewUrl, bool? IsPublished);

public sealed record AssignStudentResumeMentorRequest(string? MentorUserId);
