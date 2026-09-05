using CareerPlatform.Api.Features.Resumes.Domain;
using CareerPlatform.Api.Features.Resumes.Dto;
using CareerPlatform.Api.Infrastructure;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Resumes.Service;

/// <summary>Resume workflow. Ports the 15 legacy MediatR handlers into service methods.</summary>
internal sealed class ResumesService : IResumesService
{
    private const int RetentionDays = 30;
    private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"

    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly IResumeTextExtractor _textExtractor;
    private readonly IResumeAtsAnalyzer _atsAnalyzer;

    public ResumesService(
        AppDbContext db,
        IFileStorage storage,
        ICurrentUser currentUser,
        IResumeTextExtractor textExtractor,
        IResumeAtsAnalyzer atsAnalyzer)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
        _textExtractor = textExtractor;
        _atsAnalyzer = atsAnalyzer;
    }

    /// <summary>Minimum machine-readable words before an ATS score is meaningful.</summary>
    private const int MinAnalyzableWords = 30;

    public async Task<Result<AtsAnalysisResponse>> GetMyResumeAtsAnalysisAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<AtsAnalysisResponse>(Error.Unauthorized(
                "Resume.Unauthorized", "An authenticated user is required."));
        }

        var submission = await _db.ResumeSubmissions
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);
        if (submission is null)
        {
            return Result.Failure<AtsAnalysisResponse>(Error.NotFound(
                "Resume.NotFound", $"Resume {id} was not found."));
        }

        // Prefer the submission's own file; otherwise fall back to the student's uploaded PDF.
        var storageKey = !string.IsNullOrWhiteSpace(submission.StorageKey)
            ? submission.StorageKey
            : (await _db.StudentResumeUploads.AsNoTracking()
                .FirstOrDefaultAsync(u => u.StudentUserId == userId, ct))?.StorageKey;

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Result.Failure<AtsAnalysisResponse>(Error.Validation(
                "Resume.NoFile",
                "There is no resume file to analyse yet. Upload a resume PDF or attach a file to this resume first."));
        }

        byte[] bytes;
        try
        {
            await using var source = await _storage.OpenAsync(storageKey, ct);
            using var ms = new MemoryStream();
            await source.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }
        catch
        {
            return Result.Failure<AtsAnalysisResponse>(Error.Failure(
                "Resume.FileUnavailable", "The stored resume file could not be read. Try re-uploading it."));
        }

        var text = _textExtractor.ExtractText(bytes);
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < MinAnalyzableWords)
        {
            return Result.Failure<AtsAnalysisResponse>(Error.Validation(
                "Resume.Unreadable",
                "Could not read enough text from this resume — it may be a scanned image. Upload a text-based PDF for an ATS scan."));
        }

        var analysis = _atsAnalyzer.Analyze(text);
        submission.AtsScore = analysis.OverallScore;
        await _db.SaveChangesAsync(ct);
        return Result.Success(analysis);
    }

    // ── Student resume submissions ──────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ResumeSubmissionResponse>>> ListMyResumesAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<ResumeSubmissionResponse>>(Error.Unauthorized(
                "Resume.Unauthorized", "An authenticated user is required."));
        }
        var rows = await _db.ResumeSubmissions.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAtUtc ?? r.CreatedAtUtc)
            .ToListAsync(ct);
        IReadOnlyList<ResumeSubmissionResponse> items = rows.Select(ResumeSubmissionResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<ResumeSubmissionResponse>> CreateMyResumeAsync(
        CreateMyResumeRequest body, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<ResumeSubmissionResponse>(Error.Unauthorized(
                "Resume.Unauthorized", "An authenticated user is required."));
        }
        var code = body.TemplateCode.Trim();
        var templateOk = await _db.ResumeTemplates.AnyAsync(t => t.Code == code && t.IsPublished, ct);
        if (!templateOk)
        {
            return Result.Failure<ResumeSubmissionResponse>(Error.Validation(
                "Resume.TemplateNotFound", $"Template '{code}' does not exist or is not published."));
        }
        var s = new ResumeSubmission
        {
            UserId = userId,
            Title = body.Title.Trim(),
            TemplateCode = code,
            StorageKey = body.StorageKey?.Trim() ?? string.Empty,
        };
        _db.ResumeSubmissions.Add(s);
        await _db.SaveChangesAsync(ct);
        return Result.Success(ResumeSubmissionResponse.From(s));
    }

    public async Task<Result<ResumeSubmissionResponse>> UpdateMyResumeAsync(
        int id, UpdateMyResumeRequest body, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<ResumeSubmissionResponse>(Error.Unauthorized(
                "Resume.Unauthorized", "An authenticated user is required."));
        }
        var s = await _db.ResumeSubmissions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (s is null)
        {
            return Result.Failure<ResumeSubmissionResponse>(Error.NotFound(
                "Resume.NotFound", $"Resume {id} was not found."));
        }
        if (body.Title is not null) s.Title = body.Title.Trim();
        if (body.TemplateCode is not null)
        {
            var code = body.TemplateCode.Trim();
            var templateOk = await _db.ResumeTemplates.AnyAsync(t => t.Code == code && t.IsPublished, ct);
            if (!templateOk)
            {
                return Result.Failure<ResumeSubmissionResponse>(Error.Validation(
                    "Resume.TemplateNotFound", $"Template '{code}' does not exist or is not published."));
            }
            s.TemplateCode = code;
        }
        if (body.StorageKey is not null) s.StorageKey = body.StorageKey.Trim();
        await _db.SaveChangesAsync(ct);
        return Result.Success(ResumeSubmissionResponse.From(s));
    }

    public async Task<Result> DeleteMyResumeAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(Error.Unauthorized(
                "Resume.Unauthorized", "An authenticated user is required."));
        }
        var s = await _db.ResumeSubmissions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (s is null)
        {
            return Result.Failure(Error.NotFound("Resume.NotFound", $"Resume {id} was not found."));
        }
        _db.ResumeSubmissions.Remove(s);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Student PDF upload ──────────────────────────────────────────────────

    public async Task<Result<StudentResumeUploadResponse>> GetMyCurrentResumeAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<StudentResumeUploadResponse>(Error.Unauthorized(
                "Resume.Unauthorized", "An authenticated user is required."));
        }
        var row = await _db.StudentResumeUploads.AsNoTracking()
            .FirstOrDefaultAsync(x => x.StudentUserId == userId, ct);
        if (row is null)
        {
            return Result.Failure<StudentResumeUploadResponse>(Error.NotFound(
                "Resume.NotFound", "You have not uploaded a resume yet."));
        }
        return Result.Success(StudentResumeUploadResponse.From(row));
    }

    public async Task<Result<StudentResumeUploadResponse>> UploadMyResumePdfAsync(
        byte[] bytes, string originalFileName, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<StudentResumeUploadResponse>(Error.Unauthorized(
                "Resume.Unauthorized", "An authenticated user is required."));
        }
        if (bytes.Length == 0)
        {
            return Result.Failure<StudentResumeUploadResponse>(Error.Validation(
                "Resume.Empty", "The uploaded file is empty."));
        }
        if (!LooksLikePdf(bytes))
        {
            return Result.Failure<StudentResumeUploadResponse>(Error.Validation(
                "Resume.NotPdf", "Only PDF files are accepted (magic bytes did not match %PDF-)."));
        }

        // Overwrite prior upload — DB row + blob.
        var previous = await _db.StudentResumeUploads
            .FirstOrDefaultAsync(x => x.StudentUserId == userId, ct);
        if (previous is not null)
        {
            try { await _storage.DeleteAsync(previous.StorageKey, ct); }
            catch { /* non-fatal: bucket lifecycle handles orphans */ }
            _db.StudentResumeUploads.Remove(previous);
            await _db.SaveChangesAsync(ct);
        }

        var uploadedAt = DateTime.UtcNow;
        var storageKey = $"student-resumes/{userId}/{uploadedAt:yyyyMMddHHmmss}-{Guid.NewGuid():N}.pdf";
        var safeFileName = SanitizeFileName(originalFileName);
        await using (var ms = new MemoryStream(bytes, writable: false))
        {
            await _storage.SaveAsync(ms, storageKey, ct);
        }

        var upload = new StudentResumeUpload
        {
            StudentUserId = userId,
            StorageKey = storageKey,
            OriginalFileName = safeFileName,
            SizeBytes = bytes.Length,
            UploadedAtUtc = uploadedAt,
            ExpiresAtUtc = uploadedAt.AddDays(RetentionDays),
        };
        _db.StudentResumeUploads.Add(upload);
        await _db.SaveChangesAsync(ct);
        return Result.Success(StudentResumeUploadResponse.From(upload));
    }

    public async Task<Result> DeleteMyCurrentResumeAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(Error.Unauthorized(
                "Resume.Unauthorized", "An authenticated user is required."));
        }
        var row = await _db.StudentResumeUploads.FirstOrDefaultAsync(x => x.StudentUserId == userId, ct);
        if (row is null) return Result.Success();
        try { await _storage.DeleteAsync(row.StorageKey, ct); }
        catch { /* non-fatal */ }
        _db.StudentResumeUploads.Remove(row);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Templates ───────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ResumeTemplateResponse>>> ListTemplatesAsync(
        bool publishedOnly, CancellationToken ct)
    {
        var q = _db.ResumeTemplates.AsNoTracking();
        if (publishedOnly) q = q.Where(t => t.IsPublished);
        var rows = await q.OrderBy(t => t.Name)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<ResumeTemplateResponse> items = rows.Select(ResumeTemplateResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<ResumeTemplateResponse>> CreateTemplateAsync(
        CreateResumeTemplateRequest body, CancellationToken ct)
    {
        var code = body.Code.Trim();
        var dup = await _db.ResumeTemplates.AnyAsync(t => t.Code == code, ct);
        if (dup)
        {
            return Result.Failure<ResumeTemplateResponse>(Error.Validation(
                "ResumeTemplate.CodeExists", $"A resume template with code '{code}' already exists."));
        }
        var t = new ResumeTemplate
        {
            Code = code,
            Name = body.Name.Trim(),
            Description = body.Description?.Trim() ?? string.Empty,
            PreviewUrl = body.PreviewUrl?.Trim() ?? string.Empty,
            IsPublished = body.IsPublished,
        };
        _db.ResumeTemplates.Add(t);
        await _db.SaveChangesAsync(ct);
        return Result.Success(ResumeTemplateResponse.From(t));
    }

    public async Task<Result<ResumeTemplateResponse>> UpdateTemplateAsync(
        int id, UpdateResumeTemplateRequest body, CancellationToken ct)
    {
        var t = await _db.ResumeTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null)
        {
            return Result.Failure<ResumeTemplateResponse>(Error.NotFound(
                "ResumeTemplate.NotFound", $"Resume template {id} was not found."));
        }
        if (body.Code is not null)
        {
            var code = body.Code.Trim();
            if (code != t.Code)
            {
                var dup = await _db.ResumeTemplates.AnyAsync(x => x.Code == code && x.Id != id, ct);
                if (dup)
                {
                    return Result.Failure<ResumeTemplateResponse>(Error.Validation(
                        "ResumeTemplate.CodeExists",
                        $"A different template already uses code '{code}'."));
                }
                t.Code = code;
            }
        }
        if (body.Name is not null) t.Name = body.Name.Trim();
        if (body.Description is not null) t.Description = body.Description;
        if (body.PreviewUrl is not null) t.PreviewUrl = body.PreviewUrl;
        if (body.IsPublished is not null) t.IsPublished = body.IsPublished.Value;
        await _db.SaveChangesAsync(ct);
        return Result.Success(ResumeTemplateResponse.From(t));
    }

    public async Task<Result<ResumeTemplateResponse>> GetTemplateByIdAsync(int id, CancellationToken ct)
    {
        var t = await _db.ResumeTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return Result.Failure<ResumeTemplateResponse>(Error.NotFound(
            "ResumeTemplate.NotFound", $"Resume template {id} was not found."));
        return Result.Success(ResumeTemplateResponse.From(t));
    }

    public async Task<Result> DeleteTemplateAsync(int id, CancellationToken ct)
    {
        var t = await _db.ResumeTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null)
        {
            return Result.Failure(Error.NotFound(
                "ResumeTemplate.NotFound", $"Resume template {id} was not found."));
        }
        _db.ResumeTemplates.Remove(t);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Admin — student uploads ─────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<StudentResumeUploadResponse>>> ListStudentResumesAsync(
        bool? onlyUnassigned, CancellationToken ct)
    {
        var query = _db.StudentResumeUploads.AsNoTracking();
        if (onlyUnassigned == true)
        {
            query = query.Where(x => x.AssignedMentorUserId == null);
        }
        var rows = await (
            from upload in query
            join student in _db.UserProfiles.AsNoTracking() on upload.StudentUserId equals student.Id into studentJoin
            from student in studentJoin.DefaultIfEmpty()
            join mentor in _db.UserProfiles.AsNoTracking() on upload.AssignedMentorUserId equals mentor.Id into mentorJoin
            from mentor in mentorJoin.DefaultIfEmpty()
            orderby upload.UploadedAtUtc descending
            select new
            {
                Upload = upload,
                StudentName = student != null ? student.FullName : null,
                StudentEmail = student != null ? student.Email : null,
                MentorName = mentor != null ? mentor.FullName : null,
            })
            .ToListAsync(ct);
        IReadOnlyList<StudentResumeUploadResponse> items = rows
            .Select(r => StudentResumeUploadResponse.From(
                r.Upload, r.StudentName, r.StudentEmail, r.MentorName))
            .ToList();
        return Result.Success(items);
    }

    public async Task<Result<StudentResumeUploadResponse>> AssignMentorAsync(
        int id, string? mentorUserId, CancellationToken ct)
    {
        var upload = await _db.StudentResumeUploads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (upload is null)
        {
            return Result.Failure<StudentResumeUploadResponse>(Error.NotFound(
                "Resume.NotFound", $"Resume upload {id} was not found."));
        }
        string? mentorFullName = null;
        if (string.IsNullOrWhiteSpace(mentorUserId))
        {
            upload.AssignedMentorUserId = null;
            upload.AssignedAtUtc = null;
        }
        else
        {
            var identifier = mentorUserId.Trim();
            var looksLikeEmail = identifier.Contains('@', StringComparison.Ordinal);
            var mentor = looksLikeEmail
                ? await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(u => u.Email == identifier, ct)
                : await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(u => u.Id == identifier, ct);
            if (mentor is null)
            {
                return Result.Failure<StudentResumeUploadResponse>(Error.Validation(
                    "Resume.MentorNotFound", $"Mentor '{identifier}' does not exist."));
            }
            if (!string.Equals(mentor.Role, "Mentor", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<StudentResumeUploadResponse>(Error.Validation(
                    "Resume.NotAMentor", $"User '{identifier}' is not a Mentor (role='{mentor.Role}')."));
            }
            upload.AssignedMentorUserId = mentor.Id;
            upload.AssignedAtUtc = DateTime.UtcNow;
            mentorFullName = mentor.FullName;
        }
        await _db.SaveChangesAsync(ct);
        var student = await _db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == upload.StudentUserId, ct);
        return Result.Success(StudentResumeUploadResponse.From(
            upload, student?.FullName, student?.Email, mentorFullName));
    }

    public async Task<Result<ResumeDownloadPayload>> DownloadStudentResumeAsync(
        int id, bool allowAdmin, CancellationToken ct)
    {
        var upload = await _db.StudentResumeUploads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (upload is null)
        {
            return Result.Failure<ResumeDownloadPayload>(Error.NotFound(
                "Resume.NotFound", $"Resume upload {id} was not found."));
        }
        // Mentor route: only the assigned mentor may download.
        if (!allowAdmin)
        {
            var caller = _currentUser.UserId;
            if (string.IsNullOrEmpty(caller) ||
                !string.Equals(upload.AssignedMentorUserId, caller, StringComparison.Ordinal))
            {
                // 404 (not 403) so an unassigned mentor cannot enumerate resume ids.
                return Result.Failure<ResumeDownloadPayload>(Error.NotFound(
                    "Resume.NotFound", $"Resume upload {id} was not found."));
            }
        }
        await using var source = await _storage.OpenAsync(upload.StorageKey, ct);
        using var ms = new MemoryStream();
        await source.CopyToAsync(ms, ct);
        return Result.Success(new ResumeDownloadPayload(
            ms.ToArray(), upload.OriginalFileName, "application/pdf"));
    }

    // ── Mentor ──────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<StudentResumeUploadResponse>>> ListMyAssignedResumesAsync(CancellationToken ct)
    {
        var mentorId = _currentUser.UserId;
        if (string.IsNullOrEmpty(mentorId))
        {
            return Result.Failure<IReadOnlyList<StudentResumeUploadResponse>>(Error.Unauthorized(
                "Resume.Unauthorized", "An authenticated user is required."));
        }
        var rows = await (
            from upload in _db.StudentResumeUploads.AsNoTracking()
            where upload.AssignedMentorUserId == mentorId
            join student in _db.UserProfiles.AsNoTracking() on upload.StudentUserId equals student.Id into studentJoin
            from student in studentJoin.DefaultIfEmpty()
            orderby upload.AssignedAtUtc descending, upload.UploadedAtUtc descending
            select new
            {
                Upload = upload,
                StudentName = student != null ? student.FullName : null,
                StudentEmail = student != null ? student.Email : null,
            })
            .ToListAsync(ct);
        IReadOnlyList<StudentResumeUploadResponse> items = rows
            .Select(r => StudentResumeUploadResponse.From(
                r.Upload, r.StudentName, r.StudentEmail, mentorFullName: null))
            .ToList();
        return Result.Success(items);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static bool LooksLikePdf(byte[] bytes)
    {
        if (bytes.Length < PdfMagic.Length) return false;
        for (var i = 0; i < PdfMagic.Length; i++)
        {
            if (bytes[i] != PdfMagic[i]) return false;
        }
        return true;
    }

    private static string SanitizeFileName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "resume.pdf";
        var name = Path.GetFileName(raw.Trim());
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }
        if (name.Length > 255) name = name[..255];
        return name;
    }
}
