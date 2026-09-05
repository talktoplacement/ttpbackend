using CareerPlatform.Api.Features.LearningPaths.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class LearningPathConfiguration : IEntityTypeConfiguration<LearningPath>
{
    public void Configure(EntityTypeBuilder<LearningPath> builder)
    {
        builder.ToTable("LearningPaths");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.TargetRole);
        builder.HasIndex(p => p.IsPublished);
        builder.Ignore(p => p.DomainEvents);
    }
}
