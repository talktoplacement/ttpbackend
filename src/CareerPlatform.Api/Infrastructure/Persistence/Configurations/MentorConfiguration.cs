using CareerPlatform.Api.Features.Mentorship.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>Mentor</c> model config: int PK by convention; the Mentor→MentorSlot
/// relationship is discovered by annotation/convention as in the legacy model (Req 18.1, 18.2).
/// </summary>
public sealed class MentorConfiguration : IEntityTypeConfiguration<Mentor>
{
    public void Configure(EntityTypeBuilder<Mentor> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Ignore(m => m.DomainEvents);

        // Links the catalog row to the authenticated mentor user (nullable, indexed for the
        // mentor-self lookups). MaxLength mirrors other UserId columns (Supabase GUID / email).
        builder.Property(m => m.UserId).HasMaxLength(64);
        builder.HasIndex(m => m.UserId);
    }
}
