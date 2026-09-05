using CareerPlatform.Api.Features.Content.Dto;
using CareerPlatform.Api.Features.Content.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Content.Controller;

/// <summary>
/// Admin content-management surface — languages, sections, topics, and reorder/publish/price
/// mutations. Dual-route: canonical <c>/api/v1/content-management</c> + legacy
/// <c>/api/ContentManagement</c>.
/// </summary>
[ApiController]
[Route("api/v1/content-management")]   // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class ContentController : ControllerBase
{
    private readonly IContentService _service;
    public ContentController(IContentService service) => _service = service;

    // ── Languages ───────────────────────────────────────────────────────────

    /// <summary>GET /languages — all languages ordered by title.</summary>
    [HttpGet("languages")]
    public async Task<ActionResult<IReadOnlyList<LanguageResponse>>> GetLanguages(CancellationToken ct)
        => (await _service.GetAllLanguagesAsync(ct)).ToActionResult();

    /// <summary>GET /languages/{id} — a single track (any publish state) for the edit form.</summary>
    [HttpGet("languages/{id:int}")]
    public async Task<ActionResult<LanguageResponse>> GetLanguage(int id, CancellationToken ct)
        => (await _service.GetLanguageByIdAsync(id, ct)).ToActionResult();

    /// <summary>POST /languages — create a language.</summary>
    [HttpPost("languages")]
    public async Task<ActionResult<LanguageResponse>> CreateLanguage(
        [FromBody] CreateLanguageRequest body, CancellationToken ct)
        => (await _service.CreateLanguageAsync(body, ct)).ToActionResult();

    /// <summary>PUT /languages/{id} — partial update of a track's editable metadata.</summary>
    [HttpPut("languages/{id:int}")]
    public async Task<ActionResult<LanguageResponse>> UpdateLanguage(
        int id, [FromBody] UpdateLanguageRequest body, CancellationToken ct)
        => (await _service.UpdateLanguageAsync(id, body, ct)).ToActionResult();

    /// <summary>PUT /languages/{id}/publish — toggle a language's public visibility.</summary>
    [HttpPut("languages/{id:int}/publish")]
    public async Task<ActionResult> SetPublished(
        int id, [FromBody] SetPublishedRequest body, CancellationToken ct)
        => (await _service.SetLanguagePublishedAsync(id, body.IsPublished, ct)).ToActionResult();

    /// <summary>PUT /languages/{id}/price — update the language price.</summary>
    [HttpPut("languages/{id:int}/price")]
    public async Task<ActionResult> UpdatePrice(
        int id, [FromBody] UpdateLanguagePriceRequest body, CancellationToken ct)
        => (await _service.UpdateLanguagePriceAsync(id, body.Price, ct)).ToActionResult();

    /// <summary>PUT /languages/{languageId}/sections/reorder — new section ordering.</summary>
    [HttpPut("languages/{languageId:int}/sections/reorder")]
    public async Task<ActionResult> ReorderSections(
        int languageId, [FromBody] ReorderRequest body, CancellationToken ct)
        => (await _service.ReorderSectionsAsync(languageId, body.OrderedIds, ct)).ToActionResult();

    // ── Sections ────────────────────────────────────────────────────────────

    /// <summary>POST /sections — create a section under a language; returns the new id.</summary>
    [HttpPost("sections")]
    public async Task<ActionResult<int>> CreateSection(
        [FromBody] CreateSectionRequest body, CancellationToken ct)
        => (await _service.CreateSectionAsync(body, ct)).ToActionResult();

    /// <summary>PUT /sections/{sectionId}/topics/reorder — new topic ordering.</summary>
    [HttpPut("sections/{sectionId:int}/topics/reorder")]
    public async Task<ActionResult> ReorderTopics(
        int sectionId, [FromBody] ReorderRequest body, CancellationToken ct)
        => (await _service.ReorderTopicsAsync(sectionId, body.OrderedIds, ct)).ToActionResult();

    // ── Topics ──────────────────────────────────────────────────────────────

    /// <summary>GET /topics/{id} — topic detail (with section/language slug).</summary>
    [HttpGet("topics/{id:int}")]
    public async Task<ActionResult<TopicDetailResponse>> GetTopic(int id, CancellationToken ct)
        => (await _service.GetTopicByIdAsync(id, ct)).ToActionResult();

    /// <summary>POST /topics — create a topic; returns the new id.</summary>
    [HttpPost("topics")]
    public async Task<ActionResult<int>> CreateTopic(
        [FromBody] CreateTopicRequest body, CancellationToken ct)
        => (await _service.CreateTopicAsync(body, ct)).ToActionResult();

    /// <summary>PUT /topics/{id} — update the editable fields on a topic.</summary>
    [HttpPut("topics/{id:int}")]
    public async Task<ActionResult> UpdateTopic(
        int id, [FromBody] UpdateTopicRequest body, CancellationToken ct)
        => (await _service.UpdateTopicAsync(id, body, ct)).ToActionResult();

    /// <summary>DELETE /topics/{id} — remove a topic.</summary>
    [HttpDelete("topics/{id:int}")]
    public async Task<ActionResult> DeleteTopic(int id, CancellationToken ct)
        => (await _service.DeleteTopicAsync(id, ct)).ToActionResult();
}

/// <summary>
/// Public curriculum surface — the one anonymous read on this feature. Rate limit intentionally
/// disabled to match legacy behavior.
/// </summary>
[ApiController]
[Route("api/v1/curriculum")]                         // canonical
[Produces("application/json")]
[AllowAnonymous]
public sealed class PublicCurriculumController : ControllerBase
{
    private readonly IContentService _service;
    public PublicCurriculumController(IContentService service) => _service = service;

    /// <summary>GET /{langSlug} — published curriculum tree for a language slug.</summary>
    [HttpGet("{langSlug}")]
    public async Task<ActionResult<CurriculumResponse>> Get(string langSlug, CancellationToken ct)
        => (await _service.GetPublicCurriculumAsync(langSlug, ct)).ToActionResult();
}

/// <summary>
/// Admin bulk-import surface — accepts an .xlsx workbook and upserts interview questions.
/// Mounted on the canonical <c>/api/v1/admin/content-management/import</c> route; the legacy
/// PascalCase alias has been removed.
/// </summary>
[ApiController]
[Route("api/v1/admin/content-management/import")]     // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class ContentImportController : ControllerBase
{
    /// <summary>8 MB cap — anything larger is likely a mistake and should be split.</summary>
    private const long MaxUploadBytes = 8L * 1024 * 1024;

    private readonly IContentService _service;
    public ContentImportController(IContentService service) => _service = service;

    /// <summary>POST /interview — upload an .xlsx workbook of interview questions to import.</summary>
    [HttpPost("interview")]
    [DisableRequestSizeLimit]
    [Consumes("multipart/form-data")]
    // No [FromForm] on the IFormFile: the parameter name already selects the "file" form field, and
    // combining the attribute with IFormFile makes Swashbuckle throw, which took the whole
    // /swagger/v1/swagger.json document down with a 500.
    public async Task<IActionResult> ImportInterview(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Problem(
                title: "No file uploaded",
                detail: "Attach an .xlsx workbook in the 'file' form field.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        if (file.Length > MaxUploadBytes)
        {
            return Problem(
                title: "File too large",
                detail: $"Maximum upload size is {MaxUploadBytes / (1024 * 1024)} MB.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var result = await _service.ImportInterviewQuestionsAsync(ms.ToArray(), file.FileName, ct);
        if (result.IsSuccess) return Ok(result.Value);
        var status = Common.ResultHttpExtensions.StatusMap[result.Error.Type];
        return Problem(title: result.Error.Code, detail: result.Error.Message, statusCode: status);
    }
}
