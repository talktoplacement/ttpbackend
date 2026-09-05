using CareerPlatform.Api.Features.Cms.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class CmsTestimonialConfiguration : IEntityTypeConfiguration<CmsTestimonial>
{
    public void Configure(EntityTypeBuilder<CmsTestimonial> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.AuthorName).IsRequired().HasMaxLength(128);
        b.Property(x => x.AuthorRole).HasMaxLength(200);
        b.Property(x => x.Quote).IsRequired().HasMaxLength(2000);
        b.Property(x => x.AvatarUrl).HasMaxLength(500);
        b.HasIndex(x => new { x.IsPublished, x.DisplayOrder });
    }
}
