using CareerPlatform.Api.Features.Resumes.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core model for <see cref="StudentResumeUpload"/>. The unique index on StudentUserId is
/// what enforces "only one upload per student" at the DB layer — the ingestion handler still
/// deletes the old row first, but this index is the safety net if two uploads race.
/// </summary>
public sealed class StudentResumeUploadConfiguration : IEntityTypeConfiguration<StudentResumeUpload>
{
    public void Configure(EntityTypeBuilder<StudentResumeUpload> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.StudentUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.AssignedMentorUserId).HasMaxLength(64);

        builder.HasIndex(x => x.StudentUserId).IsUnique();
        builder.HasIndex(x => x.AssignedMentorUserId);
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
