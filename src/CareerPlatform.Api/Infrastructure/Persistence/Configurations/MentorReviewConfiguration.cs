using CareerPlatform.Api.Features.Mentorship.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core model for <see cref="MentorReview"/>: int PK, FK to <see cref="Mentor"/> with cascade
/// delete, and an index on <c>MentorId</c> for the mentor-self feedback query. Column shapes match
/// the hand-maintained <c>MentorReviews</c> table in <c>schema.sql</c>.
/// </summary>
public sealed class MentorReviewConfiguration : IEntityTypeConfiguration<MentorReview>
{
    public void Configure(EntityTypeBuilder<MentorReview> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Ignore(r => r.DomainEvents);

        builder.Property(r => r.StudentUserId).IsRequired().HasMaxLength(64);
        builder.Property(r => r.StudentName).HasMaxLength(150);
        builder.Property(r => r.Comment).HasMaxLength(2000);

        builder
            .HasOne(r => r.Mentor)
            .WithMany()
            .HasForeignKey(r => r.MentorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.MentorId);

        // One review per session. Filtered so the legacy rows with a null BookingId don't collide.
        builder.HasIndex(r => r.BookingId)
            .IsUnique()
            .HasFilter("\"BookingId\" IS NOT NULL");
    }
}
