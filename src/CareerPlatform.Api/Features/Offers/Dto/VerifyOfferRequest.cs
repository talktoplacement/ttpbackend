namespace CareerPlatform.Api.Features.Offers.Dto;

/// <summary>
/// Inbound body for <c>POST /api/v1/admin/offers/{id}/verify</c>. Only <c>Status</c> is
/// client-settable; the target offer id is carried on the URL. <c>Status</c> must be
/// <c>"Verified"</c> or <c>"Rejected"</c> — enforced by <c>VerifyOfferRequestValidator</c>.
/// </summary>
public sealed record VerifyOfferRequest(string Status);
