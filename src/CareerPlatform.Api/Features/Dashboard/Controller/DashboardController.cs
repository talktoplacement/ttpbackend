using CareerPlatform.Api.Features.Dashboard.Dto;
using CareerPlatform.Api.Features.Dashboard.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Dashboard.Controller;

[ApiController]
[Route("api/v1/admin/dashboard")]     // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;
    public DashboardController(IDashboardService service) => _service = service;

    /// <summary>GET /stats — student counts by plan + revenue rollup for the window.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsResponse>> Stats(
        [FromQuery] string? filter, CancellationToken ct)
    {
        var result = await _service.GetAdminStatsAsync(filter ?? "month", ct);
        return result.ToActionResult();
    }

    /// <summary>GET /students — registered students newest-first.</summary>
    [HttpGet("students")]
    public async Task<ActionResult<IReadOnlyList<RegisteredStudentResponse>>> Students(CancellationToken ct)
    {
        var result = await _service.GetRegisteredStudentsAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>GET /students/{id} — a single registered student's profile.</summary>
    [HttpGet("students/{id}")]
    public async Task<ActionResult<RegisteredStudentResponse>> Student(string id, CancellationToken ct)
    {
        var result = await _service.GetStudentByIdAsync(id, ct);
        return result.ToActionResult();
    }

    /// <summary>GET /student-performance — last-30-day watch minutes + completed modules.</summary>
    [HttpGet("student-performance")]
    public async Task<ActionResult<IReadOnlyList<StudentPerformanceResponse>>> StudentPerformance(CancellationToken ct)
    {
        var result = await _service.GetStudentPerformanceAsync(ct);
        return result.ToActionResult();
    }
}

/// <summary>Admin business-analytics surface — real revenue + enrollment time-series.</summary>
[ApiController]
[Route("api/v1/admin/analytics")]     // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminAnalyticsController : ControllerBase
{
    private readonly IDashboardService _service;
    public AdminAnalyticsController(IDashboardService service) => _service = service;

    /// <summary>GET /overview — trailing-window revenue + enrollment series and totals.</summary>
    [HttpGet("overview")]
    public async Task<ActionResult<AnalyticsOverviewResponse>> Overview(
        [FromQuery] int months = 6, CancellationToken ct = default)
        => (await _service.GetAnalyticsOverviewAsync(months, ct)).ToActionResult();
}
