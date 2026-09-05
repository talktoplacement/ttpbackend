using CareerPlatform.Api.Features.Offers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>OfferLetter</c> model config: int PK by convention; the
/// OfferLetter→UserProfile relationship is discovered by annotation as in the legacy model
/// (Req 18.1, 18.2).
/// </summary>
public sealed class OfferLetterConfiguration : IEntityTypeConfiguration<OfferLetter>
{
    public void Configure(EntityTypeBuilder<OfferLetter> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Ignore(o => o.DomainEvents);
    }
}
