using CareerPlatform.Api.Features.Offers.Domain;
using CareerPlatform.Api.Features.Offers.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Offers.Service;

/// <summary>
/// Admin offer-verification workflow. Ports the two legacy MediatR handlers verbatim:
/// <c>GetPendingOffersHandler</c> and <c>VerifyOfferHandler</c>. All EF queries stay parameterized
/// and async; the entity is never leaked.
/// </summary>
internal sealed class OfferService : IOfferService
{
    private readonly AppDbContext _db;

    public OfferService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<PendingOfferResponse>>> ListPendingAsync(CancellationToken ct)
    {
        var offers = await _db.OfferLetters
            .Include(o => o.User)
            .Where(o => o.Status == "Pending")
            .OrderByDescending(o => o.UploadedAt)
            .ToListAsync(ct);

        IReadOnlyList<PendingOfferResponse> response = offers.Select(ToPendingResponse).ToList();
        return Result.Success(response);
    }

    public async Task<Result<VerifyOfferResponse>> VerifyAsync(int offerId, string status, CancellationToken ct)
    {
        var offer = await _db.OfferLetters.FindAsync(new object[] { offerId }, ct);
        if (offer is null)
        {
            return Result.Failure<VerifyOfferResponse>(Error.NotFound(
                "Offer.NotFound", $"Offer {offerId} was not found."));
        }

        offer.Status = status;
        await _db.SaveChangesAsync(ct);

        return Result.Success(new VerifyOfferResponse(
            offer.Id, $"Offer {status.ToLower()} successfully"));
    }

    private static PendingOfferResponse ToPendingResponse(OfferLetter offer) =>
        new(
            offer.Id,
            offer.User != null ? offer.User.FullName : "Unknown",
            offer.CompanyName,
            offer.FileUrl,
            offer.Status,
            offer.UploadedAt.ToString("dd MMM yyyy HH:mm"));
}
