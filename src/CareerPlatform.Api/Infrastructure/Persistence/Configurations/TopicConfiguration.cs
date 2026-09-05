using CareerPlatform.Api.Features.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core model for <see cref="Topic"/>. Enforces the length constraints from data-annotations
/// and indexes the parent Section for the ordered curriculum lookup used on every read.
/// </summary>
public sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Ignore(t => t.DomainEvents);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(160);
        builder.Property(t => t.Content).IsRequired();

        builder.Property(t => t.CompanyTags).HasMaxLength(500);
        builder.Property(t => t.Frequency).HasMaxLength(32);
        builder.Property(t => t.Difficulty).HasMaxLength(32);

        builder.HasIndex(t => new { t.SectionId, t.OrderIndex });
        builder.HasIndex(t => new { t.SectionId, t.Slug }).IsUnique();
    }
}
