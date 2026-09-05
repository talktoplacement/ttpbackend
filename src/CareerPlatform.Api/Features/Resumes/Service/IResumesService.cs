using CareerPlatform.Api.Features.Resumes.Dto;

namespace CareerPlatform.Api.Features.Resumes.Service;

public interface IResumesService
{
    // Student — resume submissions (metadata)
    Task<Result<IReadOnlyList<ResumeSubmissionResponse>>> ListMyResumesAsync(CancellationToken ct);
    Task<Result<ResumeSubmissionResponse>> CreateMyResumeAsync(CreateMyResumeRequest body, CancellationToken ct);
    Task<Result<ResumeSubmissionResponse>> UpdateMyResumeAsync(int id, UpdateMyResumeRequest body, CancellationToken ct);
    Task<Result> DeleteMyResumeAsync(int id, CancellationToken ct);

    /// <summary>Deterministic ATS scan of the caller's resume (submission id), persisting the score.</summary>
    Task<Result<AtsAnalysisResponse>> GetMyResumeAtsAnalysisAsync(int id, CancellationToken ct);

    // Student — current PDF upload
    Task<Result<StudentResumeUploadResponse>> GetMyCurrentResumeAsync(CancellationToken ct);
    Task<Result<StudentResumeUploadResponse>> UploadMyResumePdfAsync(byte[] bytes, string originalFileName, CancellationToken ct);
    Task<Result> DeleteMyCurrentResumeAsync(CancellationToken ct);

    // Templates
    Task<Result<IReadOnlyList<ResumeTemplateResponse>>> ListTemplatesAsync(bool publishedOnly, CancellationToken ct);
    Task<Result<ResumeTemplateResponse>> GetTemplateByIdAsync(int id, CancellationToken ct);
    Task<Result<ResumeTemplateResponse>> CreateTemplateAsync(CreateResumeTemplateRequest body, CancellationToken ct);
    Task<Result<ResumeTemplateResponse>> UpdateTemplateAsync(int id, UpdateResumeTemplateRequest body, CancellationToken ct);
    Task<Result> DeleteTemplateAsync(int id, CancellationToken ct);

    // Admin — student uploads
    Task<Result<IReadOnlyList<StudentResumeUploadResponse>>> ListStudentResumesAsync(bool? onlyUnassigned, CancellationToken ct);
    Task<Result<StudentResumeUploadResponse>> AssignMentorAsync(int id, string? mentorUserId, CancellationToken ct);
    Task<Result<ResumeDownloadPayload>> DownloadStudentResumeAsync(int id, bool allowAdmin, CancellationToken ct);

    // Mentor
    Task<Result<IReadOnlyList<StudentResumeUploadResponse>>> ListMyAssignedResumesAsync(CancellationToken ct);
}
