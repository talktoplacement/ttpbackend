using CareerPlatform.Api.Features.Meetings.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for admin-scheduled <see cref="Meeting"/>: bounded string lengths, a
/// scheduled-time index for the upcoming-first grid, and domain-events ignored.
/// </summary>
public sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("AdminMeetings");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.MeetingType).IsRequired().HasMaxLength(64);
        builder.Property(m => m.MentorName).HasMaxLength(200);
        builder.Property(m => m.MentorCompany).HasMaxLength(200);
        builder.Property(m => m.StudentName).HasMaxLength(200);
        builder.Property(m => m.StudentEmail).HasMaxLength(200);
        builder.Property(m => m.CohortTarget).HasMaxLength(32);
        builder.Property(m => m.TargetAudienceLabel).HasMaxLength(200);
        builder.Property(m => m.Status).IsRequired().HasMaxLength(32);
        builder.Property(m => m.MeetUrl).HasMaxLength(500);
        builder.Property(m => m.Notes).HasMaxLength(2000);

        builder.HasIndex(m => m.ScheduledAtUtc);
        builder.HasIndex(m => m.Status);

        builder.Ignore(m => m.DomainEvents);
    }
}
