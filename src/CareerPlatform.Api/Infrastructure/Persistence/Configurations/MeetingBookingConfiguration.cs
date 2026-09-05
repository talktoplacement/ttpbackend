using CareerPlatform.Api.Features.Mentorship.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>MeetingBooking</c> model config: int PK by convention. The 1:1
/// relationship to <see cref="MentorSlot"/> is configured on the MentorSlot side, exactly as in
/// the legacy model (Req 18.1, 18.2).
/// </summary>
public sealed class MeetingBookingConfiguration : IEntityTypeConfiguration<MeetingBooking>
{
    public void Configure(EntityTypeBuilder<MeetingBooking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Ignore(b => b.DomainEvents);
    }
}
