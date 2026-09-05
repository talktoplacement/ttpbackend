using CareerPlatform.Api.Features.Cms.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class CmsBannerConfiguration : IEntityTypeConfiguration<CmsBanner>
{
    public void Configure(EntityTypeBuilder<CmsBanner> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Message).IsRequired().HasMaxLength(500);
        b.Property(x => x.LinkUrl).HasMaxLength(500);
        b.Property(x => x.Tone).IsRequired().HasMaxLength(16);
        b.HasIndex(x => x.DisplayOrder);
    }
}
