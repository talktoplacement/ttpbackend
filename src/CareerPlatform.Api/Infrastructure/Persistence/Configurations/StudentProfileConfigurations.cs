using CareerPlatform.Api.Features.StudentProfile.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class StudentEducationConfiguration : IEntityTypeConfiguration<StudentEducation>
{
    public void Configure(EntityTypeBuilder<StudentEducation> b)
    {
        b.ToTable("StudentEducations");
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);

        b.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        b.Property(x => x.Degree).IsRequired().HasMaxLength(200);
        b.Property(x => x.Institution).IsRequired().HasMaxLength(200);
        b.Property(x => x.FieldOfStudy).HasMaxLength(160);
        b.Property(x => x.GradeScale).HasMaxLength(16);
        b.Property(x => x.Description).HasMaxLength(1000);
        // Matches the DDL: two decimals covers both a 9.25 CGPA and an 82.50 percentage.
        b.Property(x => x.GradeValue).HasColumnType("numeric(6,2)");

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.UserId, x.DisplayOrder });
    }
}

public sealed class StudentPreferencesConfiguration : IEntityTypeConfiguration<StudentPreferences>
{
    public void Configure(EntityTypeBuilder<StudentPreferences> b)
    {
        b.ToTable("StudentPreferences");
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);

        b.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        b.Property(x => x.PreferredRole).HasMaxLength(160);
        b.Property(x => x.PreferredLocations).HasMaxLength(500);

        // One row per user — the service's get-or-create depends on this holding.
        b.HasIndex(x => x.UserId).IsUnique();
    }
}
