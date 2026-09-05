using CareerPlatform.Api.Features.Products.Dto;
using CareerPlatform.Api.Features.Products.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Products.Controller;

/// <summary>Admin CRUD for products (one-time SKUs and add-ons).</summary>
[ApiController]
[Route("api/v1/admin/products")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IProductService _service;
    public AdminProductsController(IProductService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List(
        [FromQuery(Name = "activeOnly")] bool activeOnly, CancellationToken ct)
        => (await _service.ListAsync(activeOnly, ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(
        int id, [FromBody] UpdateProductRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
