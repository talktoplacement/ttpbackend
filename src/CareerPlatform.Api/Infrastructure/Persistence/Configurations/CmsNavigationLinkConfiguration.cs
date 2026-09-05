using CareerPlatform.Api.Features.Cms.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class CmsNavigationLinkConfiguration : IEntityTypeConfiguration<CmsNavigationLink>
{
    public void Configure(EntityTypeBuilder<CmsNavigationLink> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Label).IsRequired().HasMaxLength(64);
        b.Property(x => x.Href).IsRequired().HasMaxLength(500);
        b.Property(x => x.GroupName).IsRequired().HasMaxLength(32);
        b.HasIndex(x => new { x.GroupName, x.DisplayOrder });
    }
}
