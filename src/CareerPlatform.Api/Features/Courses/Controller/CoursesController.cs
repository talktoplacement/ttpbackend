using CareerPlatform.Api.Features.Courses.Dto;
using CareerPlatform.Api.Features.Courses.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Courses.Controller;

/// <summary>
/// HTTP surface for the Courses feature. Two routes mount on this controller so both mobile
/// (canonical <c>/api/v1/courses</c>) and the existing web frontend (legacy <c>/api/Courses</c>)
/// hit the same actions — no middleware coupling required. Every action is thin: bind, delegate
/// to <see cref="ICourseService"/>, translate the <see cref="Result{T}"/> to an HTTP response.
/// </summary>
[ApiController]
[Route("api/v1/courses")]     // canonical URL (mobile clients + OpenAPI-generated code)
[Produces("application/json")]
public sealed class CoursesController : ControllerBase
{
    private readonly ICourseService _service;
    private readonly IStudentCourseService _studentCourses;

    public CoursesController(ICourseService service, IStudentCourseService studentCourses)
    {
        _service = service;
        _studentCourses = studentCourses;
    }

    /// <summary>GET  <c>/api/v1/courses/catalog</c> — public catalog (published courses, price-ascending).</summary>
    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CourseResponse>>> ListPublished(CancellationToken ct)
    {
        var result = await _service.ListPublishedAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// GET <c>/api/v1/courses/me</c> — the signed-in student's course library with progress.
    ///
    /// Declared before the <c>{id:int}</c> admin route is irrelevant to matching (the int constraint
    /// excludes "me"), but it is placed here to keep the two student-visible reads together.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<MyCourseResponse>>> ListMine(CancellationToken ct)
        => (await _studentCourses.ListMineAsync(ct)).ToActionResult();

    /// <summary>GET  <c>/api/v1/courses</c> — admin listing, paginated.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PaginatedResult<CourseResponse>>> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var result = await _service.ListAllAsync(page, pageSize, ct);
        return result.ToActionResult();
    }

    /// <summary>GET  <c>/api/v1/courses/{id}</c> — admin single-course fetch.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<CourseResponse>> Get(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.ToActionResult();
    }

    /// <summary>POST  <c>/api/v1/courses</c> — admin create.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<CourseResponse>> Create(
        [FromBody] CreateCourseRequest body,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(body, ct);
        return result.ToActionResult();
    }

    /// <summary>PUT  <c>/api/v1/courses/{id}</c> — admin update.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<CourseResponse>> Update(
        int id,
        [FromBody] UpdateCourseRequest body,
        CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, body, ct);
        return result.ToActionResult();
    }

    /// <summary>DELETE  <c>/api/v1/courses/{id}</c> — admin delete-or-archive.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        return result.ToActionResult();
    }
}
