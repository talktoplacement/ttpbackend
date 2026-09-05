namespace CareerPlatform.Api.Features.Offers.Dto;

/// <summary>Confirmation payload returned by the verify-offer endpoint.</summary>
public sealed record VerifyOfferResponse(int OfferId, string Message);
