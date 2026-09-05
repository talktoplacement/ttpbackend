using CareerPlatform.Api.Features.MentorAssignments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class MentorAssignmentConfiguration : IEntityTypeConfiguration<MentorAssignment>
{
    public void Configure(EntityTypeBuilder<MentorAssignment> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        // IsActive is a computed convenience property, not a column.
        b.Ignore(x => x.IsActive);
        b.Property(x => x.StudentUserId).IsRequired().HasMaxLength(64);
        b.Property(x => x.CohortName).HasMaxLength(128);
        b.Property(x => x.Notes).HasMaxLength(1000);

        // Partial unique index: at most one ACTIVE assignment per student. Matches the
        // `WHERE "EndedAtUtc" IS NULL` filtered index in schema.sql.
        b.HasIndex(x => x.StudentUserId)
            .IsUnique()
            .HasFilter("\"EndedAtUtc\" IS NULL");

        b.HasIndex(x => x.MentorId);
    }
}
