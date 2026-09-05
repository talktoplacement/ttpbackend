using CareerPlatform.Api.Features.Certificates.Dto;
using CareerPlatform.Api.Features.Certificates.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Certificates.Controller;

[ApiController]
[Route("api/v1/certificates")]   // canonical
[Produces("application/json")]
public sealed class CertificatesController : ControllerBase
{
    private readonly ICertificateService _service;
    public CertificatesController(ICertificateService service) => _service = service;

    /// <summary>GET /me — list my active certificates.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<CertificateResponse>>> ListMine(CancellationToken ct)
    {
        var result = await _service.ListMineAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// GET /verify/{code} — public verification lookup. Rate-limited (<c>Sensitive</c> policy)
    /// because the endpoint is unauthenticated and would otherwise let an attacker enumerate
    /// valid <see cref="Domain.Certificate.VerificationCode"/> values at line rate.
    /// </summary>
    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<CertificateResponse>> Verify(string code, CancellationToken ct)
    {
        var result = await _service.VerifyAsync(code, ct);
        return result.ToActionResult();
    }

    /// <summary>GET /{id}/download — student-owner downloads their own PDF.</summary>
    [HttpGet("{id:int}/download")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var result = await _service.DownloadAsync(id, allowAdmin: false, ct);
        if (result.IsSuccess)
        {
            return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
        }
        var status = Common.ResultHttpExtensions.StatusMap[result.Error.Type];
        return Problem(title: result.Error.Code, detail: result.Error.Message, statusCode: status);
    }
}

/// <summary>Admin surface for certificate issuance and admin downloads.</summary>
[ApiController]
[Route("api/v1/admin/certificates")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminCertificatesController : ControllerBase
{
    private readonly ICertificateService _service;
    public AdminCertificatesController(ICertificateService service) => _service = service;

    /// <summary>POST — issue a new certificate to a student.</summary>
    [HttpPost]
    public async Task<ActionResult<CertificateResponse>> Issue(
        [FromBody] IssueCertificateRequest body, CancellationToken ct)
    {
        var result = await _service.IssueAsync(body, ct);
        return result.ToActionResult();
    }

    /// <summary>GET /{id}/download — admin download of any certificate PDF.</summary>
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var result = await _service.DownloadAsync(id, allowAdmin: true, ct);
        if (result.IsSuccess)
        {
            return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
        }
        var status = Common.ResultHttpExtensions.StatusMap[result.Error.Type];
        return Problem(title: result.Error.Code, detail: result.Error.Message, statusCode: status);
    }
}
