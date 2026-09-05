using CareerPlatform.Api.Features.Coupons.Dto;
using CareerPlatform.Api.Features.Coupons.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Coupons.Controller;

/// <summary>Admin surface for discount-coupon CRUD.</summary>
[ApiController]
[Route("api/v1/admin/coupons")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminCouponsController : ControllerBase
{
    private readonly ICouponService _service;
    public AdminCouponsController(ICouponService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CouponResponse>>> List(CancellationToken ct)
        => (await _service.ListAsync(ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CouponResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<CouponResponse>> Create(
        [FromBody] CreateCouponRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CouponResponse>> Update(
        int id, [FromBody] UpdateCouponRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
