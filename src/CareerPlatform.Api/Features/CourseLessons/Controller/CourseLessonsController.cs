using CareerPlatform.Api.Features.CourseLessons.Dto;
using CareerPlatform.Api.Features.CourseLessons.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.CourseLessons.Controller;

/// <summary>Student lesson player: published lessons joined with the caller's progress.</summary>
[ApiController]
[Route("api/v1/courses/{courseId:int}/lessons")]
[Produces("application/json")]
[Authorize]
public sealed class CourseLessonsController : ControllerBase
{
    private readonly ICourseLessonService _service;
    public CourseLessonsController(ICourseLessonService service) => _service = service;

    /// <summary>GET — lesson list + per-lesson progress + course rollup for the caller.</summary>
    [HttpGet]
    public async Task<ActionResult<CourseLessonsWithProgressResponse>> List(
        int courseId, CancellationToken ct)
        => (await _service.ListForStudentAsync(courseId, ct)).ToActionResult();
}

/// <summary>Admin lesson authoring for a course.</summary>
[ApiController]
[Route("api/v1/admin/courses/{courseId:int}/lessons")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminCourseLessonsController : ControllerBase
{
    private readonly ICourseLessonService _service;
    public AdminCourseLessonsController(ICourseLessonService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseLessonResponse>>> List(
        int courseId, CancellationToken ct)
        => (await _service.ListForAdminAsync(courseId, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<CourseLessonResponse>> Create(
        int courseId, [FromBody] CreateCourseLessonRequest body, CancellationToken ct)
        => (await _service.CreateAsync(courseId, body, ct)).ToActionResult();

    [HttpPut("{lessonId:int}")]
    public async Task<ActionResult<CourseLessonResponse>> Update(
        int courseId, int lessonId,
        [FromBody] UpdateCourseLessonRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(courseId, lessonId, body, ct)).ToActionResult();

    [HttpDelete("{lessonId:int}")]
    public async Task<ActionResult> Delete(int courseId, int lessonId, CancellationToken ct)
        => (await _service.DeleteAsync(courseId, lessonId, ct)).ToActionResult();

    /// <summary>PUT /reorder — persist a new lesson order for the course.</summary>
    [HttpPut("reorder")]
    public async Task<ActionResult> Reorder(
        int courseId, [FromBody] ReorderCourseLessonsRequest body, CancellationToken ct)
        => (await _service.ReorderAsync(courseId, body, ct)).ToActionResult();
}
