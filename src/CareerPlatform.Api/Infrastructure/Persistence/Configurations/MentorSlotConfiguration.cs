using CareerPlatform.Api.Features.Mentorship.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>MentorSlot</c> model config verbatim, including the required 1:1
/// MentorSlot↔MeetingBooking relationship keyed by <c>MeetingBooking.SlotId</c> with cascade
/// delete (Req 18.1, 18.2, 24.5).
/// </summary>
public sealed class MentorSlotConfiguration : IEntityTypeConfiguration<MentorSlot>
{
    public void Configure(EntityTypeBuilder<MentorSlot> builder)
    {
        builder.HasKey(s => s.Id);

        builder
            .HasOne(s => s.Booking)
            .WithOne(b => b.Slot)
            .HasForeignKey<MeetingBooking>(b => b.SlotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
    }
}
