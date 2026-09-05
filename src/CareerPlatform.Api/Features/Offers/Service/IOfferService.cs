using CareerPlatform.Api.Features.Offers.Dto;

namespace CareerPlatform.Api.Features.Offers.Service;

/// <summary>
/// Public contract for the admin offer-verification workflow. All operations are admin-scoped;
/// consumed by <see cref="Controller.OffersController"/>.
/// </summary>
public interface IOfferService
{
    /// <summary>Lists every offer with <c>Status = "Pending"</c>, newest-first.</summary>
    Task<Result<IReadOnlyList<PendingOfferResponse>>> ListPendingAsync(CancellationToken ct);

    /// <summary>Sets an offer's <c>Status</c> to Verified or Rejected. NotFound when the id is unknown.</summary>
    Task<Result<VerifyOfferResponse>> VerifyAsync(int offerId, string status, CancellationToken ct);
}
