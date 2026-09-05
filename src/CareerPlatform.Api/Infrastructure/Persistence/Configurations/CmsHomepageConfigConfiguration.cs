using CareerPlatform.Api.Features.Cms.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class CmsHomepageConfigConfiguration : IEntityTypeConfiguration<CmsHomepageConfig>
{
    public void Configure(EntityTypeBuilder<CmsHomepageConfig> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        // Singleton row: id is assigned explicitly (never auto-generated).
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.HeroTitle).IsRequired().HasMaxLength(200);
        b.Property(x => x.HeroSubtitle).HasMaxLength(500);
        b.Property(x => x.PrimaryCtaLabel).HasMaxLength(64);
        b.Property(x => x.PrimaryCtaHref).HasMaxLength(500);
        b.Property(x => x.SecondaryCtaLabel).HasMaxLength(64);
        b.Property(x => x.SecondaryCtaHref).HasMaxLength(500);
    }
}
