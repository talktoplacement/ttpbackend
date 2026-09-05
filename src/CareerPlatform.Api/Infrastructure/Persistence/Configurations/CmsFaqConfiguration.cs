using CareerPlatform.Api.Features.Cms.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class CmsFaqConfiguration : IEntityTypeConfiguration<CmsFaq>
{
    public void Configure(EntityTypeBuilder<CmsFaq> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Question).IsRequired().HasMaxLength(500);
        b.Property(x => x.Answer).IsRequired().HasMaxLength(4000);
        b.HasIndex(x => new { x.IsPublished, x.DisplayOrder });
    }
}
