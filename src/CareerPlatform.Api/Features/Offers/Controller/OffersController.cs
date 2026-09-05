using CareerPlatform.Api.Features.Offers.Dto;
using CareerPlatform.Api.Features.Offers.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Offers.Controller;

/// <summary>
/// Admin surface for the offer-verification workflow, mounted on the canonical
/// <c>/api/v1/admin/offers</c> URL. The legacy PascalCase alias has been removed.
/// </summary>
[ApiController]
[Route("api/v1/admin/offers")]   // canonical URL (mobile clients + OpenAPI-generated code)
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class OffersController : ControllerBase
{
    private readonly IOfferService _service;

    public OffersController(IOfferService service) => _service = service;

    /// <summary>GET <c>/api/v1/admin/offers/pending</c> — every offer awaiting verification, newest first.</summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<PendingOfferResponse>>> ListPending(CancellationToken ct)
    {
        var result = await _service.ListPendingAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>POST <c>/api/v1/admin/offers/{id}/verify</c> — set an offer's status to Verified or Rejected.</summary>
    [HttpPost("{id:int}/verify")]
    public async Task<ActionResult<VerifyOfferResponse>> Verify(
        int id, [FromBody] VerifyOfferRequest body, CancellationToken ct)
    {
        var result = await _service.VerifyAsync(id, body.Status, ct);
        return result.ToActionResult();
    }
}
