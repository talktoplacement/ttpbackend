using CareerPlatform.Api.Features.PracticeBanks.Dto;
using CareerPlatform.Api.Features.PracticeBanks.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.PracticeBanks.Controller;

/// <summary>Public practice-bank catalog + bank detail with ordered questions.</summary>
[ApiController]
[Route("api/v1/practice-banks")]
[Produces("application/json")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class PracticeBanksController : ControllerBase
{
    private readonly IPracticeBankService _service;
    public PracticeBanksController(IPracticeBankService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PracticeBankResponse>>> List(CancellationToken ct)
        => (await _service.ListPublishedAsync(ct)).ToActionResult();

    [HttpGet("{slug}")]
    public async Task<ActionResult<PracticeBankDetailResponse>> Get(string slug, CancellationToken ct)
        => (await _service.GetBySlugAsync(slug, ct)).ToActionResult();
}

/// <summary>Admin bank CRUD + membership management.</summary>
[ApiController]
[Route("api/v1/admin/practice-banks")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminPracticeBanksController : ControllerBase
{
    private readonly IPracticeBankService _service;
    public AdminPracticeBanksController(IPracticeBankService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PracticeBankResponse>>> List(CancellationToken ct)
        => (await _service.ListAllAsync(ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PracticeBankResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<PracticeBankResponse>> Create(
        [FromBody] CreatePracticeBankRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PracticeBankResponse>> Update(
        int id, [FromBody] UpdatePracticeBankRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();

    /// <summary>PUT /{id}/questions — replaces membership; array order becomes display order.</summary>
    [HttpPut("{id:int}/questions")]
    public async Task<ActionResult<PracticeBankDetailResponse>> SetQuestions(
        int id, [FromBody] SetBankQuestionsRequest body, CancellationToken ct)
        => (await _service.SetQuestionsAsync(id, body, ct)).ToActionResult();
}
