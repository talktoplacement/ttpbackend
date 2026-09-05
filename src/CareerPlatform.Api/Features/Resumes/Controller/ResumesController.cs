using CareerPlatform.Api.Features.Resumes.Dto;
using CareerPlatform.Api.Features.Resumes.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Resumes.Controller;

/// <summary>Student self-service resume surface — metadata submissions + PDF upload.</summary>
[ApiController]
[Route("api/v1/resumes/me")]  // canonical
[Produces("application/json")]
[Authorize]
public sealed class MyResumesController : ControllerBase
{
    /// <summary>1 MB PDF cap — enforced at the endpoint before the body is read.</summary>
    public const long MaxUploadBytes = 1L * 1024 * 1024;

    private readonly IResumesService _service;
    public MyResumesController(IResumesService service) => _service = service;

    /// <summary>GET — every resume submission owned by the caller.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResumeSubmissionResponse>>> List(CancellationToken ct)
        => (await _service.ListMyResumesAsync(ct)).ToActionResult();

    /// <summary>POST — create a new resume submission for the caller.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<ResumeSubmissionResponse>> Create(
        [FromBody] CreateMyResumeRequest body, CancellationToken ct)
        => (await _service.CreateMyResumeAsync(body, ct)).ToActionResult();

    /// <summary>PUT /{id} — owner-only partial update.</summary>
    [HttpPut("{id:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<ResumeSubmissionResponse>> Update(
        int id, [FromBody] UpdateMyResumeRequest body, CancellationToken ct)
        => (await _service.UpdateMyResumeAsync(id, body, ct)).ToActionResult();

    /// <summary>DELETE /{id} — owner-only delete of a resume submission.</summary>
    [HttpDelete("{id:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteMyResumeAsync(id, ct)).ToActionResult();

    /// <summary>GET /{id}/ats-analysis — deterministic ATS scan of the caller's resume.</summary>
    [HttpGet("{id:int}/ats-analysis")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<AtsAnalysisResponse>> AtsAnalysis(int id, CancellationToken ct)
        => (await _service.GetMyResumeAtsAnalysisAsync(id, ct)).ToActionResult();

    /// <summary>GET /current — the caller's current PDF upload metadata.</summary>
    [HttpGet("current")]
    public async Task<ActionResult<StudentResumeUploadResponse>> Current(CancellationToken ct)
        => (await _service.GetMyCurrentResumeAsync(ct)).ToActionResult();

    /// <summary>DELETE /current — remove the caller's current PDF (blob + row).</summary>
    [HttpDelete("current")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> DeleteCurrent(CancellationToken ct)
        => (await _service.DeleteMyCurrentResumeAsync(ct)).ToActionResult();

    /// <summary>POST /upload-pdf — multipart <c>file</c> field, application/pdf ≤ 1 MB.</summary>
    [HttpPost("upload-pdf")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    [DisableRequestSizeLimit]
    [Consumes("multipart/form-data")]
    // No [FromForm] on the IFormFile: the parameter name already selects the "file" form field, and
    // combining the attribute with IFormFile makes Swashbuckle throw, taking the whole OpenAPI
    // document down with a 500.
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Problem(
                title: "No file uploaded",
                detail: "Attach a PDF in the 'file' form field.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        if (file.Length > MaxUploadBytes)
        {
            return Problem(
                title: "File too large",
                detail: "Maximum upload size is 1 MB.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var result = await _service.UploadMyResumePdfAsync(ms.ToArray(), file.FileName, ct);
        if (result.IsSuccess) return Ok(result.Value);
        var status = Common.ResultHttpExtensions.StatusMap[result.Error.Type];
        return Problem(title: result.Error.Code, detail: result.Error.Message, statusCode: status);
    }
}

/// <summary>Resume-template read surface — authenticated catalog of published templates.</summary>
[ApiController]
[Route("api/v1/resume-templates")]  // canonical
[Produces("application/json")]
[Authorize]
public sealed class ResumeTemplatesController : ControllerBase
{
    private readonly IResumesService _service;
    public ResumeTemplatesController(IResumesService service) => _service = service;

    /// <summary>GET — published templates ordered by name.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResumeTemplateResponse>>> List(CancellationToken ct)
        => (await _service.ListTemplatesAsync(publishedOnly: true, ct)).ToActionResult();
}

/// <summary>Resume-template admin CRUD surface.</summary>
[ApiController]
[Route("api/v1/admin/resume-templates")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminResumeTemplatesController : ControllerBase
{
    private readonly IResumesService _service;
    public AdminResumeTemplatesController(IResumesService service) => _service = service;

    /// <summary>GET — all templates (published + unpublished).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResumeTemplateResponse>>> List(CancellationToken ct)
        => (await _service.ListTemplatesAsync(publishedOnly: false, ct)).ToActionResult();

    /// <summary>GET /{id} — single template for the edit form.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResumeTemplateResponse>> GetById(int id, CancellationToken ct)
        => (await _service.GetTemplateByIdAsync(id, ct)).ToActionResult();

    /// <summary>POST — create a new template.</summary>
    [HttpPost]
    public async Task<ActionResult<ResumeTemplateResponse>> Create(
        [FromBody] CreateResumeTemplateRequest body, CancellationToken ct)
        => (await _service.CreateTemplateAsync(body, ct)).ToActionResult();

    /// <summary>PUT /{id} — partial update.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ResumeTemplateResponse>> Update(
        int id, [FromBody] UpdateResumeTemplateRequest body, CancellationToken ct)
        => (await _service.UpdateTemplateAsync(id, body, ct)).ToActionResult();

    /// <summary>DELETE /{id} — remove a template.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteTemplateAsync(id, ct)).ToActionResult();
}

/// <summary>Admin surface for reviewing student resume uploads.</summary>
[ApiController]
[Route("api/v1/admin/student-resumes")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminStudentResumesController : ControllerBase
{
    private readonly IResumesService _service;
    public AdminStudentResumesController(IResumesService service) => _service = service;

    /// <summary>GET — every stored student PDF, joined with student/mentor names.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentResumeUploadResponse>>> List(
        [FromQuery] bool? onlyUnassigned, CancellationToken ct)
        => (await _service.ListStudentResumesAsync(onlyUnassigned, ct)).ToActionResult();

    /// <summary>PUT /{id}/assign-mentor — set/clear the reviewing mentor.</summary>
    [HttpPut("{id:int}/assign-mentor")]
    public async Task<ActionResult<StudentResumeUploadResponse>> AssignMentor(
        int id, [FromBody] AssignStudentResumeMentorRequest body, CancellationToken ct)
        => (await _service.AssignMentorAsync(id, body.MentorUserId, ct)).ToActionResult();

    /// <summary>GET /{id}/download — stream the PDF for admin review.</summary>
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var result = await _service.DownloadStudentResumeAsync(id, allowAdmin: true, ct);
        if (result.IsSuccess)
        {
            return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
        }
        var status = Common.ResultHttpExtensions.StatusMap[result.Error.Type];
        return Problem(title: result.Error.Code, detail: result.Error.Message, statusCode: status);
    }
}

/// <summary>Mentor surface for reviewing resumes assigned to the caller.</summary>
[ApiController]
[Route("api/v1/mentor/student-resumes")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Mentor")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class MentorStudentResumesController : ControllerBase
{
    private readonly IResumesService _service;
    public MentorStudentResumesController(IResumesService service) => _service = service;

    /// <summary>GET — resume uploads currently assigned to the authenticated mentor.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentResumeUploadResponse>>> List(CancellationToken ct)
        => (await _service.ListMyAssignedResumesAsync(ct)).ToActionResult();

    /// <summary>GET /{id}/download — stream the PDF; only assigned mentor may download.</summary>
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var result = await _service.DownloadStudentResumeAsync(id, allowAdmin: false, ct);
        if (result.IsSuccess)
        {
            return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
        }
        var status = Common.ResultHttpExtensions.StatusMap[result.Error.Type];
        return Problem(title: result.Error.Code, detail: result.Error.Message, statusCode: status);
    }
}
