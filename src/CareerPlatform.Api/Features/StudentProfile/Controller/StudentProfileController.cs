using CareerPlatform.Api.Features.StudentProfile.Dto;
using CareerPlatform.Api.Features.StudentProfile.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.StudentProfile.Controller;

/// <summary>
/// Qualifications on the caller's own profile. Mounted under <c>/api/v1/me</c> alongside
/// <c>/api/v1/me/skills</c>, so every self-service profile resource shares one prefix and the
/// authenticated user is always implicit in the route.
/// </summary>
[ApiController]
[Route("api/v1/me/education")]
[Produces("application/json")]
[Authorize]
public sealed class MyEducationController : ControllerBase
{
    private readonly IStudentProfileService _service;
    public MyEducationController(IStudentProfileService service) => _service = service;

    /// <summary>GET — the caller's qualifications plus the grade scales the API accepts.</summary>
    [HttpGet]
    public async Task<ActionResult<EducationListResponse>> List(CancellationToken ct)
        => (await _service.ListMyEducationAsync(ct)).ToActionResult();

    /// <summary>POST — adds a qualification.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<EducationResponse>> Create(
        [FromBody] UpsertEducationRequest body, CancellationToken ct)
        => (await _service.AddMyEducationAsync(body, ct)).ToActionResult();

    /// <summary>PUT — replaces one of the caller's qualifications.</summary>
    [HttpPut("{id:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<EducationResponse>> Update(
        int id, [FromBody] UpsertEducationRequest body, CancellationToken ct)
        => (await _service.UpdateMyEducationAsync(id, body, ct)).ToActionResult();

    /// <summary>DELETE — removes one of the caller's qualifications.</summary>
    [HttpDelete("{id:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteMyEducationAsync(id, ct)).ToActionResult();
}

/// <summary>Notification and visibility preferences for the caller.</summary>
[ApiController]
[Route("api/v1/me/preferences")]
[Produces("application/json")]
[Authorize]
public sealed class MyPreferencesController : ControllerBase
{
    private readonly IStudentProfileService _service;
    public MyPreferencesController(IStudentProfileService service) => _service = service;

    /// <summary>
    /// GET — the caller's preferences. Returns documented defaults (rather than 404) for a student
    /// who has never saved any, so the client never has to invent a starting state.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PreferencesResponse>> Get(CancellationToken ct)
        => (await _service.GetMyPreferencesAsync(ct)).ToActionResult();

    /// <summary>PUT — full replacement; every switch must be sent explicitly.</summary>
    [HttpPut]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PreferencesResponse>> Update(
        [FromBody] UpdatePreferencesRequest body, CancellationToken ct)
        => (await _service.UpdateMyPreferencesAsync(body, ct)).ToActionResult();
}
