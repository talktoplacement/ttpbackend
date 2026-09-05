using CareerPlatform.Api.Features.Cms.Dto;
using CareerPlatform.Api.Features.Cms.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Cms.Controller;

/// <summary>Public (anonymous) CMS reads. Rate-limited to prevent scraping.</summary>
[ApiController]
[Route("api/v1/cms")]
[Produces("application/json")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class CmsController : ControllerBase
{
    private readonly ICmsService _service;
    public CmsController(ICmsService service) => _service = service;

    [HttpGet("faqs")]
    public async Task<ActionResult<IReadOnlyList<CmsFaqResponse>>> ListFaqs(CancellationToken ct)
        => (await _service.ListPublishedFaqsAsync(ct)).ToActionResult();

    [HttpGet("testimonials")]
    public async Task<ActionResult<IReadOnlyList<CmsTestimonialResponse>>> ListTestimonials(CancellationToken ct)
        => (await _service.ListPublishedTestimonialsAsync(ct)).ToActionResult();

    /// <summary>GET /navigation?group=header|footer|mobile — defaults to <c>header</c>.</summary>
    [HttpGet("navigation")]
    public async Task<ActionResult<IReadOnlyList<CmsNavigationLinkResponse>>> ListNavigation(
        [FromQuery(Name = "group")] string? group, CancellationToken ct)
        => (await _service.ListPublishedNavigationAsync(group ?? "header", ct)).ToActionResult();
}

/// <summary>Admin CMS CRUD across FAQs, testimonials, and navigation links.</summary>
[ApiController]
[Route("api/v1/admin/cms")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminCmsController : ControllerBase
{
    private readonly ICmsService _service;
    public AdminCmsController(ICmsService service) => _service = service;

    // FAQs
    [HttpGet("faqs")]
    public async Task<ActionResult<IReadOnlyList<CmsFaqResponse>>> ListFaqs(CancellationToken ct)
        => (await _service.ListAllFaqsAsync(ct)).ToActionResult();

    [HttpGet("faqs/{id:int}")]
    public async Task<ActionResult<CmsFaqResponse>> GetFaq(int id, CancellationToken ct)
        => (await _service.GetFaqAsync(id, ct)).ToActionResult();

    [HttpPost("faqs")]
    public async Task<ActionResult<CmsFaqResponse>> CreateFaq(
        [FromBody] UpsertCmsFaqRequest body, CancellationToken ct)
        => (await _service.CreateFaqAsync(body, ct)).ToActionResult();

    [HttpPut("faqs/{id:int}")]
    public async Task<ActionResult<CmsFaqResponse>> UpdateFaq(
        int id, [FromBody] UpsertCmsFaqRequest body, CancellationToken ct)
        => (await _service.UpdateFaqAsync(id, body, ct)).ToActionResult();

    [HttpDelete("faqs/{id:int}")]
    public async Task<ActionResult> DeleteFaq(int id, CancellationToken ct)
        => (await _service.DeleteFaqAsync(id, ct)).ToActionResult();

    // Testimonials
    [HttpGet("testimonials")]
    public async Task<ActionResult<IReadOnlyList<CmsTestimonialResponse>>> ListTestimonials(CancellationToken ct)
        => (await _service.ListAllTestimonialsAsync(ct)).ToActionResult();

    [HttpGet("testimonials/{id:int}")]
    public async Task<ActionResult<CmsTestimonialResponse>> GetTestimonial(int id, CancellationToken ct)
        => (await _service.GetTestimonialAsync(id, ct)).ToActionResult();

    [HttpPost("testimonials")]
    public async Task<ActionResult<CmsTestimonialResponse>> CreateTestimonial(
        [FromBody] UpsertCmsTestimonialRequest body, CancellationToken ct)
        => (await _service.CreateTestimonialAsync(body, ct)).ToActionResult();

    [HttpPut("testimonials/{id:int}")]
    public async Task<ActionResult<CmsTestimonialResponse>> UpdateTestimonial(
        int id, [FromBody] UpsertCmsTestimonialRequest body, CancellationToken ct)
        => (await _service.UpdateTestimonialAsync(id, body, ct)).ToActionResult();

    [HttpDelete("testimonials/{id:int}")]
    public async Task<ActionResult> DeleteTestimonial(int id, CancellationToken ct)
        => (await _service.DeleteTestimonialAsync(id, ct)).ToActionResult();

    // Navigation
    [HttpGet("navigation")]
    public async Task<ActionResult<IReadOnlyList<CmsNavigationLinkResponse>>> ListNavigation(CancellationToken ct)
        => (await _service.ListAllNavigationAsync(ct)).ToActionResult();

    [HttpGet("navigation/{id:int}")]
    public async Task<ActionResult<CmsNavigationLinkResponse>> GetNavigation(int id, CancellationToken ct)
        => (await _service.GetNavigationAsync(id, ct)).ToActionResult();

    [HttpPost("navigation")]
    public async Task<ActionResult<CmsNavigationLinkResponse>> CreateNavigation(
        [FromBody] UpsertCmsNavigationLinkRequest body, CancellationToken ct)
        => (await _service.CreateNavigationAsync(body, ct)).ToActionResult();

    [HttpPut("navigation/{id:int}")]
    public async Task<ActionResult<CmsNavigationLinkResponse>> UpdateNavigation(
        int id, [FromBody] UpsertCmsNavigationLinkRequest body, CancellationToken ct)
        => (await _service.UpdateNavigationAsync(id, body, ct)).ToActionResult();

    [HttpDelete("navigation/{id:int}")]
    public async Task<ActionResult> DeleteNavigation(int id, CancellationToken ct)
        => (await _service.DeleteNavigationAsync(id, ct)).ToActionResult();
}
