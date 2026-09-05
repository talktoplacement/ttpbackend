using CareerPlatform.Api.Features.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class LearningProgressConfiguration : IEntityTypeConfiguration<LearningProgress>
{
    public void Configure(EntityTypeBuilder<LearningProgress> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ResourceType).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ResourceType);
        builder.HasIndex(x => new { x.UserId, x.ResourceType, x.ResourceId }).IsUnique();
    }
}
