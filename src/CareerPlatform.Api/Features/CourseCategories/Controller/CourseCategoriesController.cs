using CareerPlatform.Api.Features.CourseCategories.Dto;
using CareerPlatform.Api.Features.CourseCategories.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.CourseCategories.Controller;

/// <summary>Public: browse categories for catalog filtering.</summary>
[ApiController]
[Route("api/v1/course-categories")]
[Produces("application/json")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class CourseCategoriesController : ControllerBase
{
    private readonly ICourseCategoryService _service;
    public CourseCategoriesController(ICourseCategoryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseCategoryResponse>>> List(CancellationToken ct)
        => (await _service.ListPublishedAsync(ct)).ToActionResult();
}

/// <summary>Admin CRUD for course categories.</summary>
[ApiController]
[Route("api/v1/admin/course-categories")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminCourseCategoriesController : ControllerBase
{
    private readonly ICourseCategoryService _service;
    public AdminCourseCategoriesController(ICourseCategoryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseCategoryResponse>>> List(CancellationToken ct)
        => (await _service.ListAllAsync(ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseCategoryResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<CourseCategoryResponse>> Create(
        [FromBody] CreateCourseCategoryRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CourseCategoryResponse>> Update(
        int id, [FromBody] UpdateCourseCategoryRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
